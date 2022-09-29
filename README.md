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

#Docker compose region
gubben:
    image: discordgubbbot:latest
    restart: unless-stopped
    container_name: gubben
    hostname: gubben
    volumes:
      - /etc/gubben/:/etc/gubben/
      - /etc/localtime:/etc/localtime:ro
    environment:
      - TZ=Europe/Stockholm
      - SSH_URL=
      - SSH_USERNAME=
      - SSH_PASSWORD=
      - LOGIN_TOKEN=
      - GOOGLE_APPLICATION_CREDENTIALS=/etc/gubben/google_auth.json
