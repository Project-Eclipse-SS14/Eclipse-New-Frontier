# HASH: 0cc5774f1b6e32eeed2e41811ee87ef60cb43acefe6706d5eb0f2b1bd1020a4e
ent-BaseGenerator = generator
    .desc = A high efficiency thermoelectric generator.
# HASH: 75141be088f12fb9555940df0f5cfd8eb9740670fb7765ba5fae5c868c0ea661
ent-BaseGeneratorWallmount = wallmount generator
    .desc = A high efficiency thermoelectric generator stuffed in a wall cabinet.
# HASH: 4b5b93b3a0532b212068878c35fcf3f40d55e147f4794cd47d2aa9886085a427
ent-BaseGeneratorWallmountFrame = wallmount generator frame
    .desc = A construction frame for a wallmount generator.
# HASH: 8738dd43702c58e703758ed32c9c338d8a0df844e4ab3b5d849e32cffacb45f1
ent-GeneratorBasic = { ent-BaseGenerator }
    .desc = { ent-BaseGenerator.desc }
    .suffix = Basic, 3kW
# HASH: e06f2df4526821c773c768286039a1167eb933fdef5b11e093deaac91fed5f1b
ent-GeneratorBasic15kW = { ent-BaseGenerator }
    .desc = { ent-BaseGenerator.desc }
    .suffix = Basic, 15kW, Anchored
# HASH: ebdb1d5b000f38958f0275d4adc47d56a0899ef7741168940f5d982119eaadb6
ent-GeneratorWallmountBasic = { ent-BaseGeneratorWallmount }
    .desc = { ent-BaseGeneratorWallmount.desc }
    .suffix = Basic, 3kW
# HASH: e43172fda55ab5be55128826a85b28cf1671f180bf20d836e8ed59a0993fc850
ent-GeneratorWallmountAPU = shuttle APU
    .desc = An auxiliary power unit for a shuttle - 6kW.
    .suffix = APU, 6kW
# HASH: 9a10acdca787d63d5cf07f7d2768c63ba214b8940688c0b6b763cc619bb35611
ent-GeneratorRTG = RTG
    .desc = A Radioisotope Thermoelectric Generator for long term power.
    .suffix = 10kW
# HASH: 197204f2c78a816565a05b4ad30b9c24280ff85030e6c75ff4e964f2e1b1a590
ent-GeneratorRTGDamaged = damaged RTG
    .desc = A Radioisotope Thermoelectric Generator for long term power. This one has damaged shielding.
    .suffix = 10kW
