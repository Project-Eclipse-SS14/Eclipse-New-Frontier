# HASH: e0547b46c84667228bd9fd4d3ea10507231351a1c18630cc55d840300266f577
ent-BasePlantAnalyzer = plant analyzer
    .desc = A handheld device that allows you to scan seeds and plants to get detailed information about their genes.
# HASH: 022912d9b4af005679399772282b30e74a4c75d7940eef204a30eb30e8c73092
ent-PlantAnalyzer = { ent-BasePlantAnalyzer }
    .desc = { ent-BasePlantAnalyzer.desc }
# HASH: 9bbc9d57427d07c556d0d41c96a277107092fcf390efc8c8efc1446818f66dd3
ent-PlantAnalyzerEmpty = { ent-PlantAnalyzer }
    .desc = { ent-PlantAnalyzer.desc }
    .suffix = Empty
# HASH: abb712241fe773b1350ffd4427ccdd49b696949397c0bd1cb3d8355f1f6783bc
ent-PlantAnalyzerDebug = { ent-BasePlantAnalyzer }
    .desc = { ent-BasePlantAnalyzer.desc }
    .suffix = Debug
