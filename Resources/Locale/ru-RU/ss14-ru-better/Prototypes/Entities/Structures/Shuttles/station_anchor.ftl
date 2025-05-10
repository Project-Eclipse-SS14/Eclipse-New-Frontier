# HASH: 19a544f99247656db98aea31bdc5f71ec1cc2fa181b9c7db469f51ba7a980215
ent-StationAnchorBase = станционный якорь
    .desc = Предотвращает смещение станций.
    .suffix = Включен
# HASH: d9a1c028b6fc3727abd749c816295d90ae3b88fbc90e5ca522b7b74dd6c140ff
ent-StationAnchorIndestructible = { ent-StationAnchorBase }
    .desc = { ent-StationAnchorBase.desc }
    .suffix = Неразрушимый, Не требует питания
# HASH: 8aa6f5ab13ce8e098db1430cadce8e96552bbf8eadc2ec0d191176e172456fbe
ent-StationAnchor = { ent-StationAnchorBase }
    .desc = { ent-StationAnchorBase.desc }
# HASH: 8033cbb975df16bb0631bb6e07c4c4247c05917bed81b1d04a9139c890cd1215
ent-StationAnchorOff = { ent-StationAnchor }
    .desc = { ent-StationAnchor.desc }
    .suffix = Выключен
