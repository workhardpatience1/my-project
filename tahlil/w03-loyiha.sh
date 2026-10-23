#!/bin/bash
# W03 loyihasi: kataloglar, /etc arxivi, tekshiruv va bitta faylni tiklash.
# Ishga tushirish: bash w03-loyiha.sh   (natija ~/w03-natija.txt ga ham yoziladi)
# Oldin VirtualBox'da before-files snapshot oling.

run() { echo; echo "\$ $*"; eval "$@"; echo "[chiqish kodi: $?]"; }

{
echo "=== W03 loyiha — $(hostname) — $(date '+%F %T')"

echo; echo "--- B2: tashkilot kataloglari"
run sudo mkdir -p /srv/company/office /srv/company/accounts /srv/company/warehouse
run ls -la /srv/company

echo; echo "--- B3: /etc arxivi"
run mkdir -p ~/company
run sudo tar -czf ~/company/etc-$(date +%F).tar.gz /etc
run sudo chown "$USER:$USER" ~/company/etc-*.tar.gz
run ls -lh ~/company

echo; echo "--- B8.2: arxiv tarkibi va /etc hajmi"
run "tar -tzf ~/company/etc-*.tar.gz | wc -l"
run "tar -tzf ~/company/etc-*.tar.gz | grep hostname"
run sudo du -sh /etc

echo; echo "--- B4-B6, B8.3: bitta faylni tiklash"
run mkdir -p ~/restore-test
run tar -xzf ~/company/etc-*.tar.gz -C ~/restore-test etc/hostname
run cat ~/restore-test/etc/hostname
run find ~/restore-test
} 2>&1 | tee ~/w03-natija.txt
