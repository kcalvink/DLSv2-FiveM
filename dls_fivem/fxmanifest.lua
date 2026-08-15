fx_version 'cerulean'
game 'gta5'

name 'dls_fivem'
author 'kcalvink + contributors'
description 'DLSv2 FiveM compatibility resource (incremental port)'
version '0.1.0'

lua54 'yes'

shared_scripts {
    'shared/constants.lua'
}

client_scripts {
    'client/main.lua'
}

server_scripts {
    'server/main.lua'
}
