dotnet publish -c Release -o /opt/iss-tracker

sudo systemctl enable iss-tracker
sudo systemctl start iss-tracker
sudo systemctl status iss-tracker
