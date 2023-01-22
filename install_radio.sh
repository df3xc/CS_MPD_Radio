#!/bin/sh

# make this file executable : chmod 777 install_luefter.sh
# execute this script : ./install_luefter.sh

cp ./radio.service /etc/systemd/system
systemctl enable radio
systemctl start radio
systemctl status radio


