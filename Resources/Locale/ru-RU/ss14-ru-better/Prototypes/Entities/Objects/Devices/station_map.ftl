# HASH: 01c7374486e898f84c618065cc4aff968394974996c559227d7c7ad359f33366
ent-BaseHandheldStationMap = карта станции
    .desc = Отображает схему текущей станции.
# HASH: 01b973ff8ba185f358d2da2930c9677d8cf70465605504a058c98ee721e311f1
ent-HandheldStationMap = { ent-BaseHandheldStationMap }
    .desc = { ent-BaseHandheldStationMap.desc }
    .suffix = Ручной, Заряжен
# HASH: c4a8eba6c05fb05ef84a2bd9dbed745a6567cc1f2b3800ca979acaad7658e5bb
ent-HandheldStationMapEmpty = { ent-HandheldStationMap }
    .desc = { ent-HandheldStationMap.desc }
    .suffix = Ручной, Пустой
# HASH: c6d33aa8aa653656fe0a2daa87485ca541ac1b72a88185af563e3a2bfb66eb7f
ent-HandheldStationMapUnpowered = { ent-BaseHandheldStationMap }
    .desc = { ent-BaseHandheldStationMap.desc }
    .suffix = Ручной, Не требует питания
