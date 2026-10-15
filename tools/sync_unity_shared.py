#!/usr/bin/env python3
"""Copies the game logic of the phone version into the Unity project, so both are the same game.

    python3 tools/sync_unity_shared.py          # copy
    python3 tools/sync_unity_shared.py --check  # only check (CI): exit 1 if Unity is out of date

Source: ObstacleDodge.MonoGame/Shared/{Core,World}/*.cs and UI/Strings.cs
Target: ObstacleDodge.Unity/Assets/Scripts/Shared/  (+ .meta files with stable GUIDs)
Edit the files in ObstacleDodge.MonoGame/Shared, then run this script.
"""
import os
import sys
import uuid

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "ObstacleDodge.MonoGame", "Shared")
DST = os.path.join(ROOT, "ObstacleDodge.Unity", "Assets", "Scripts", "Shared")
FILES = [
    "Core/AdManager.cs", "Core/GameManager.cs", "Core/IPlatform.cs", "Core/SaveData.cs",
    "World/LevelGenerator.cs", "World/Mover.cs", "World/Obstacle.cs", "World/Obstacles.cs", "World/World.cs",
    "UI/Strings.cs",
]
HEADER = ("// AUTO-COPIED from ObstacleDodge.MonoGame/Shared/{0} by tools/sync_unity_shared.py.\n"
          "// Do not edit here: edit the original and run the script, so the phone and Unity stay the same game.\n")


def meta(path_key, folder=False):
    guid = uuid.uuid5(uuid.NAMESPACE_URL, "obstacledodge/" + path_key).hex
    if folder:
        return f"fileFormatVersion: 2\nguid: {guid}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    return (f"fileFormatVersion: 2\nguid: {guid}\nMonoImporter:\n  externalObjects: {{}}\n  serializedVersion: 2\n"
            "  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData: \n"
            "  assetBundleName: \n  assetBundleVariant: \n")


def wanted():
    out = {}
    for rel in FILES:
        with open(os.path.join(SRC, rel), encoding="utf-8") as f:
            text = f.read()
        name = os.path.basename(rel)
        out[name] = HEADER.format(rel) + text
        out[name + ".meta"] = meta("shared/" + name)
    return out


def main():
    check = "--check" in sys.argv
    files = wanted()
    stale = []
    for name, text in files.items():
        path = os.path.join(DST, name)
        current = open(path, encoding="utf-8").read() if os.path.exists(path) else None
        if current != text:
            stale.append(name)
            if not check:
                os.makedirs(DST, exist_ok=True)
                with open(path, "w", encoding="utf-8", newline="\n") as f:
                    f.write(text)
    extra = [n for n in (os.listdir(DST) if os.path.isdir(DST) else []) if n not in files]
    folder_meta = DST + ".meta"
    if not os.path.exists(folder_meta):
        stale.append("Shared.meta")
        if not check:
            with open(folder_meta, "w", encoding="utf-8", newline="\n") as f:
                f.write(meta("shared-folder", folder=True))
    if check:
        if stale or extra:
            print("Unity copy of the shared game code is out of date:", ", ".join(stale + extra))
            print("Run: python3 tools/sync_unity_shared.py")
            return 1
        print("Unity shared code is up to date.")
        return 0
    for n in extra:
        os.remove(os.path.join(DST, n))
    print(f"synced {len(FILES)} files" + (f", updated: {', '.join(stale)}" if stale else " (no changes)"))
    return 0


if __name__ == "__main__":
    sys.exit(main())
