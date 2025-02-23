ent-BaseStructureComputerWallmount = { ent-BaseStructureComputerTabletop }
    .desc =
        { ent-BaseStructureComputerTabletop.desc }
        ent-ComputerWallmountBroken = { ent-ComputerBroken }
    .suffix = Настенный
    .desc =
        { ent-ComputerBroken.desc }
        ent-ComputerWallmountFrame = { ent-ComputerTabletopFrame }
    .suffix = Настенный
    .desc = { ent-ComputerTabletopFrame.desc }
ent-ComputerWallmountFrame = computer
    .desc = { ent-BaseStructureComputerWallmount.desc }
ent-ComputerWallmountBroken = { ent-BaseStructureWallmount }
    .suffix = Wallmount
    .desc = { ent-BaseStructureWallmount.desc }
