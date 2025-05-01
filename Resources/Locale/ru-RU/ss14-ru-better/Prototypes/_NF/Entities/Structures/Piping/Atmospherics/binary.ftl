# HASH: 6a6d4ba504fc0283c1a1798d19209e57ee9f55a3d365d9ab667207773f9685f0
ent-GasPressurePumpOn = { ent-GasPressurePump }
    .desc = { ent-GasPressurePump.desc }
    .suffix = On
# HASH: 379f26145765ddec997ab72db71ac8eb480b322d815b5b279d0cd1a72989256c
ent-GasPressurePumpOnMax = { "" }
    .desc = { "" }
    .suffix = On, Max
# HASH: 5a9d0ff0f902aae6cadb33ae10003faa4e454b3e691da56410c354c45623b225
ent-GasVolumePumpOn = { ent-GasVolumePump }
    .desc = { ent-GasVolumePump.desc }
    .suffix = On
# HASH: 08b96c5dfb16c51ecf2617f76e25755c3d19151713b9c1bae4b6e4e86e1ec5fe
ent-BaseGaslock = gaslock
    .desc = { "" }
# HASH: dbeb833aa103b32803f5ffbc09dab5f647bf7025bd65d77c279a845efa058587
ent-BasePressurePumpGaslock = external gaslock
    .desc = Connects gas pipes on separate ships or stations together to allow gas transfer. Both sides must be docked and pumping in the same direction to accept flow.
# HASH: 265d56b754d38314248701674ba6c030ab751c93add7715cbfc074bbf4769d35
ent-Gaslock = { ent-BasePressurePumpGaslock }
    .desc = { ent-BasePressurePumpGaslock.desc }
# HASH: 227e1f86ca84516d0773de674c2a07237e3b43950d1fe34f7db12d2216e23470
ent-GaslockFrame = portable gaslock
    .desc = Pumps gas through. Accepts docking, but cannot dock. Both sides must be docked and pumping in the same direction for gas to flow.
