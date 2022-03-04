# gubben
Bot for discord

Set discord bot loginToken in the Dockerfile or set it as an environment variable if you don't run docker.


Automated build script 

cd ~
rm -rf ./gubben
git clone https://github.com/Simon900225/gubben.git
cd gubben/DiscordGubbBot
docker build -t discordgubbbot -f Dockerfile ..
docker save -o ./gubben.tar discordgubbbot
sudo docker load --input ./gubben.tar
cd /dockercomposes/
sudo docker-compose up -d
cd ~
rm -rf ./gubben
