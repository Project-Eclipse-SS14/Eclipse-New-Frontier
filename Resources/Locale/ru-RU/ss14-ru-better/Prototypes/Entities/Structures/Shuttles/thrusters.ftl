# HASH: ae85d1d14d45e191adcb505e4556dace9af0a530c326f679c00d60a2a2f720b9
ent-BaseThruster = thruster
    .desc = A thruster that allows a shuttle to move.
# HASH: 3b006f001b019d4ab45c28c3f54a1d5c27ebef2fdf1685aa350a800717f8385e
ent-Thruster = thruster
    .desc = { ent-BaseThruster.desc }
# HASH: 7339db607a629b1e05be4d7a5457905f3463ad6a3bf4bf498dc2054b97dc12f4
ent-ThrusterUnanchored = { ent-Thruster }
    .desc = { "" }
    .suffix = Unanchored
# HASH: 1f5f784306cad016733eda34cb46f6c82961bc965ef0582b328d97e3e9b060ce
ent-DebugThruster = { ent-BaseThruster }
    .desc = { ent-BaseThruster.desc }
    .suffix = DEBUG
# HASH: 9cf9eda509d961c724e0c3ca96bd4335a5702fff67648e237e6201c3a4f331bb
ent-Gyroscope = gyroscope
    .desc = Increases the shuttle's potential angular rotation.
# HASH: 408eefc6517357717bf790bbbe941a622da7910dd535034492b2e70874c707bd
ent-GyroscopeUnanchored = { ent-Gyroscope }
    .desc = { ent-Gyroscope.desc }
    .suffix = Unanchored
# HASH: 1f5f784306cad016733eda34cb46f6c82961bc965ef0582b328d97e3e9b060ce
ent-DebugGyroscope = { ent-BaseThruster }
    .desc = { ent-BaseThruster.desc }
    .suffix = DEBUG
