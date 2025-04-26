# HASH: 7b55607eb72f4d141b9d0b72af8e34a6824939dd1cfd577a463e1a6da8e5251e
ent-FireAxeCabinetCommand = fire axe cabinet
    .desc = There is a small label that reads "For Emergency use only" along with details for safe use of the axe. As if.
    .suffix = With Lock
# HASH: c861f289a50605859b4b9be5100909543ac9858b26be7f930bf76ea98a720f75
ent-FireAxeCabinetOpenCommand = { ent-FireAxeCabinetCommand }
    .desc = { ent-FireAxeCabinetCommand.desc }
    .suffix = Open, With Lock
# HASH: 1acb7cf6f999e957e213c119853ce9a8b52dee5bc5c0c127ad6ee5c22e41b9aa
ent-FireAxeCabinetFilledCommand = { ent-FireAxeCabinetCommand }
    .desc = { ent-FireAxeCabinetCommand.desc }
    .suffix = Filled, With Lock
# HASH: 948d01daa8c18a98aec79091203e3fd876148d4b94a6f643216f7fdb2fa807b9
ent-FireAxeCabinetFilledOpenCommand = { "" }
    .desc = { "" }
    .suffix = Filled, Open, With Lock
