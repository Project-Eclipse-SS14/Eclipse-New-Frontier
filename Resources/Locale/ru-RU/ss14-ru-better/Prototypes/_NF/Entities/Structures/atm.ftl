# HASH: c3960ea7501d2b46c1910fb1bd0c27c3bd0b3307864e2306b73762d3ee2b321e
ent-ComputerBankATMBase = { ent-BaseComputer }
    .desc = { ent-BaseComputer.desc }
# HASH: 5fc5a06fb25237a6095354786b6ab47c4d893fc7d25bdb8898329d37c53d56b5
ent-ComputerBankATMDeposit = банкомат
    .desc = Используется для ввода и вывода средств с личного банковского счета.
# HASH: 57ab7672860b51671a0cde6c09bdb9ab8cbd84431fac310faa76a57875cf330f
ent-ComputerBankATMWithdraw = банкомат для снятия
    .desc = Используется для вывода средств с личного банковского счета.
# HASH: 7ccd0b071a9fc72763c3ee9ceecc971319851cfa381f0d0fa2f593b7bea44415
ent-ComputerBankATM = { ent-ComputerBankATMDeposit }
    .desc = { ent-ComputerBankATMDeposit.desc }
# HASH: 336ebb051e53586e2d1bb0eeea8111e6c2b8c210a9c9cbc76c174a9252edd49e
ent-ComputerWithdrawBankATM = { ent-ComputerBankATMWithdraw }
    .desc = { ent-ComputerBankATMWithdraw.desc }
# HASH: 8f22c432c7064e3086691f45232240ce4a3ca37ed4248177fad8aaa411d4e6be
ent-ComputerWallmountBankATM = { ent-ComputerBankATMDeposit }
    .desc = { ent-ComputerBankATMDeposit.desc }
    .suffix = Настенный
# HASH: d7430568ecead1eb5e71ec35f868096abe4d1711ad89af6b5a15905583255770
ent-ComputerWallmountWithdrawBankATM = { ent-ComputerBankATMWithdraw }
    .desc = { ent-ComputerBankATMWithdraw.desc }
    .suffix = Настенный
# HASH: e07296fc50c3c83cfa382e6736bd1c1a704fb687751f0a23fcb1656f7105c890
ent-ComputerBlackMarketBankATM = { ent-ComputerBankATMDeposit }
    .desc = Явно модифицированный банкомат, на котором краской криво написано "НАЛОГ С ПРОДАЖ 30%"
    .suffix = Чёрный рынок
# HASH: 9b51d05399fc57f70c20cb8021627f0d118ac847f9734d55b0cf4b21651f0c44
ent-ComputerWallmountBlackMarketBankATM = { ent-ComputerBankATMDeposit }
    .desc = Явно модифицированный банкомат, на котором краской криво написано "НАЛОГ С ПРОДАЖ 30%"
    .suffix = Настенный, Чёрный рынок
# HASH: f4e98fd8ad168db04dabed45feee47270d870149be794e86226635808d38e47e
ent-BaseStationAdminBankATM = консоль станционного администрирования
    .desc = Используется для снятия или пополнения средств со счетов станции.
# HASH: 00484094d7bf241bef06ad920555980ade51a6a070bbd25f80cea5ea9edc2338
ent-StationAdminBankATMFrontier = { ent-BaseStationAdminBankATM }
    .desc = Используется для снятия или пополнения средств со счетов Аванпоста Фронтира.
    .suffix = Фронтир
# HASH: 196a38ca41a236237b6a0bc6ea8062f60d16479b326391a0215c5818c03c86a2
ent-StationAdminBankATMNfsd = { ent-BaseStationAdminBankATM }
    .desc = Используется для снятия или пополнения средств со счетов ДСБФ.
    .suffix = ДСБФ
# HASH: d79cc14a9a22be11ef44f212ebffe47f60fbe718366abf56b30a6862cb5eade4
ent-StationAdminBankATMMedical = { ent-BaseStationAdminBankATM }
    .desc = Используется для снятия или пополнения средств со счетов Медицинской Диспетчерской.
    .suffix = Медицинский
