#!/usr/bin/env python3
"""Unity .asmdef dosyalarından headless .NET projelerini üretir veya denetler.

Her Assets/TarihStrateji/Core/<Modül>/TarihStrateji.Core.<Modül>.asmdef için
Tools/HeadlessTests/CoreProjects/TarihStrateji.Core.<Modül>/TarihStrateji.Core.<Modül>.csproj
yazılır. Proje, modülün kaynaklarını doğrudan Assets altından derler ve .asmdef'teki
referansları birebir (geçişsiz) ProjectReference olarak taşır. Böylece Unity'nin göreceği
derleme grafı Unity açılmadan CI'da derlenir.

Kullanım:
  python3 Tools/HeadlessTests/sync_core_projects.py          # üret / güncelle
  python3 Tools/HeadlessTests/sync_core_projects.py --check  # farkta çıkış kodu 1

Yalnız Python 3.10+ standart kütüphanesi gerekir.
"""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
REPO = HERE.parent.parent
CORE_DIR = REPO / "Assets" / "TarihStrateji" / "Core"
OUT_DIR = HERE / "CoreProjects"
TEST_PROJECT = HERE / "TarihStrateji.Core.Tests" / "TarihStrateji.Core.Tests.csproj"
PREFIX = "TarihStrateji.Core."


def load_asmdefs() -> dict[str, list[str]]:
    modules: dict[str, list[str]] = {}
    for asmdef in sorted(CORE_DIR.glob("*/*.asmdef")):
        data = json.loads(asmdef.read_text(encoding="utf-8"))
        name = data["name"]
        expected = PREFIX + asmdef.parent.name
        if name != expected or asmdef.stem != expected:
            raise SystemExit(f"HATA: {asmdef} adı '{expected}' olmalı (bulunan: '{name}').")
        refs = data.get("references", [])
        for ref in refs:
            if not ref.startswith(PREFIX):
                raise SystemExit(f"HATA: {asmdef}: Core derlemesi yalnız Core'a başvurabilir ('{ref}').")
        modules[name] = refs
    if not modules:
        raise SystemExit(f"HATA: {CORE_DIR} altında .asmdef bulunamadı.")
    return modules


def render_module(name: str, refs: list[str]) -> str:
    module = name[len(PREFIX):]
    lines = [
        '<Project Sdk="Microsoft.NET.Sdk">',
        "  <!-- OTOMATİK ÜRETİLDİ: Tools/HeadlessTests/sync_core_projects.py. Elle düzenlemeyin;",
        f"       kaynak: Assets/TarihStrateji/Core/{module}/{name}.asmdef -->",
        "  <PropertyGroup>",
        f"    <AssemblyName>{name}</AssemblyName>",
        f"    <RootNamespace>{name}</RootNamespace>",
        "  </PropertyGroup>",
        "  <ItemGroup>",
        f'    <Compile Include="$(RepoRoot)Assets/TarihStrateji/Core/{module}/**/*.cs" />',
        "  </ItemGroup>",
    ]
    if refs:
        lines.append("  <ItemGroup>")
        for ref in refs:
            lines.append(f'    <ProjectReference Include="../{ref}/{ref}.csproj" />')
        lines.append("  </ItemGroup>")
    lines.append("</Project>")
    return "\n".join(lines) + "\n"


def render_test_references(modules: dict[str, list[str]]) -> str:
    lines = ["  <ItemGroup Label=\"CoreProjects (sync_core_projects.py)\">"]
    for name in modules:
        lines.append(f'    <ProjectReference Include="../CoreProjects/{name}/{name}.csproj" />')
    lines.append("  </ItemGroup>")
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--check", action="store_true", help="yazma; güncel değilse 1 ile çık")
    args = parser.parse_args()

    modules = load_asmdefs()
    wanted: dict[Path, str] = {}
    for name, refs in modules.items():
        wanted[OUT_DIR / name / f"{name}.csproj"] = render_module(name, refs)

    stale: list[str] = []
    for path, text in wanted.items():
        current = path.read_text(encoding="utf-8") if path.exists() else None
        if current != text:
            stale.append(str(path.relative_to(REPO)))
            if not args.check:
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text(text, encoding="utf-8", newline="\n")

    for existing in sorted(OUT_DIR.glob("*/*.csproj")):
        if existing not in wanted:
            stale.append(f"{existing.relative_to(REPO)} (karşılığı olan .asmdef yok)")
            if not args.check:
                existing.unlink()

    test_text = TEST_PROJECT.read_text(encoding="utf-8")
    block = render_test_references(modules)
    if block not in test_text:
        stale.append(f"{TEST_PROJECT.relative_to(REPO)} (CoreProjects referans bloğu güncel değil)")

    if stale:
        verb = "güncel değil" if args.check else "güncellendi"
        for item in stale:
            print(f"{verb}: {item}")
        if args.check:
            print("Çözüm: python3 Tools/HeadlessTests/sync_core_projects.py", file=sys.stderr)
            return 1
        if any("CoreProjects referans bloğu" in s for s in stale):
            print("Test projesine şu bloğu yerleştirin:\n" + block)
            return 1
    else:
        print(f"Tamam: {len(modules)} Core projesi .asmdef dosyalarıyla eşleşiyor.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
