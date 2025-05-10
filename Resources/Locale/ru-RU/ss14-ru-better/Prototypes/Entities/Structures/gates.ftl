# HASH: 8d9311fbaf19b806976aa0a5aa90c19641568cd359af14c90c69d9b542895e85
ent-LogicEmptyCircuit = пустая плата
    .desc = Кажется, чего-то не хватает.
# HASH: 7818804f0f4249a51d7e6f304aa7eada3aac128ecb6042365cb824c5c4374821
ent-BaseLogicItem = { ent-LogicEmptyCircuit }
    .desc = { ent-LogicEmptyCircuit.desc }
# HASH: 071b59c3a9dd7a4773283c0a624a8f4f7e3213e036521f6548fda41d8af46719
ent-LogicGateOr = логический элемент
    .desc = Логический элемент с двумя портами на вход и одним на выход. Можно изменить логическую операцию с помощью отвёртки.
    .suffix = Or, ИЛИ
# HASH: 40208c33194584a7a601f8697ad1d051ca943efde0382181e46261b68e95eb77
ent-LogicGateAnd = { ent-LogicGateOr }
    .desc = { ent-LogicGateOr.desc }
    .suffix = And, И
# HASH: 8b33c4304f339fb8e01709faca3d06c4aaa12d5ded26b36a888993a6a6ca26f6
ent-LogicGateXor = { ent-LogicGateOr }
    .desc = { ent-LogicGateOr.desc }
    .suffix = Xor, Исключающее ИЛИ
# HASH: 98f3645ef9e2e3e373b9c6d340bf9c9662990884f30b0dbb30578c86c027dcda
ent-LogicGateNor = { ent-LogicGateOr }
    .desc = { ent-LogicGateOr.desc }
    .suffix = Nor, ИЛИ-НЕ
# HASH: 93640fe980e62dca900ca1bd932d81ec934a6459d7cab5a740b58ccfc1c017c8
ent-LogicGateNand = { ent-LogicGateOr }
    .desc = { ent-LogicGateOr.desc }
    .suffix = Nand, И-НЕ
# HASH: 62406cc21aa9cd1aec3e5fee18d0203b578078ec54d80632f276d6c694f5b744
ent-LogicGateXnor = { ent-LogicGateOr }
    .desc = { ent-LogicGateOr.desc }
    .suffix = Xnor, Исключающее ИЛИ-НЕ
# HASH: 32da8f47a6f0054913878e7a18e3c8127a4af2e719211fe87d98a6e38cc54a61
ent-EdgeDetector = детектор сигнала
    .desc = Определяет уровень сигнала и разделяет его. Устройство игнорирует импульсные сигналы.
# HASH: ab9b1d53572d04bbe5f97598db2cf69af1cb091a95016623619b19832b2080db
ent-PowerSensor = датчик питания
    .desc = Генерирует сигналы в ответ на изменение напряжения в сети. Может циклически переключаться между напряжениями кабеля.
# HASH: b49001f3d9aa15b4c4fcd6f9f2a7b72d9824f6b40e471526bd74ca5bb3def47f
ent-MemoryCell = ячейка памяти
    .desc = Схема D-триггер защелки, хранящая сигнал, который может быть изменён в зависимости от входного и разрешающего портов.
