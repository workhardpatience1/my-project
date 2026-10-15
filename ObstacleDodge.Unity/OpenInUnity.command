#!/bin/bash
# Double-click on a Mac: opens this project straight in Unity and presses Play.
# (If macOS says it is from an unidentified developer: right-click → Open.)
cd "$(dirname "$0")" || exit 1
PROJECT="$(pwd)"
UNITY=""
for app in $(ls -d /Applications/Unity/Hub/Editor/*/Unity.app 2>/dev/null | sort -V -r); do
  UNITY="$app"; break
done
if [ -z "$UNITY" ]; then
  echo "Unity topilmadi. Unity Hub orqali Unity 6 ni o'rnating (Installs → Install Editor)."
  read -r -p "Enter bosing..."
  exit 1
fi
echo "Ochilmoqda: $UNITY"
echo "Loyiha:     $PROJECT"
open -n "$UNITY" --args -projectPath "$PROJECT" -executeMethod ObstacleDodge.EditorTools.OpenAndPlay.Run
