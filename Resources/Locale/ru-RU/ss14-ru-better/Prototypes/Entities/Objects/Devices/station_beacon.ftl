# HASH: a52fba9a9de6252497c1773f53845072a8f4d607506e3d1fec5de39e3678e91e
ent-DefaultStationBeacon = станционный маяк
    .desc = Небольшое устройство, передающее информацию на карты станций. Может быть сконфигурировано.
    .suffix = Общий
# HASH: dc9d1557b7bd0ffd28d764b2cccb0ac257bea413c8073b8c69424f3c2143cf98
ent-DefaultStationBeaconUnanchored = { ent-DefaultStationBeacon }
    .desc = { ent-DefaultStationBeacon.desc }
    .suffix = Общий, Незакреплён
# HASH: cb1774dc3505a6e0e3733730cad4570eacb7943189d6ff08ff12c6707c9b7f60
ent-StationBeaconPart = каркас станционного маяка
    .desc = Сборная конструкция, используемая для создания станционного маяка.
# HASH: e233bb5f1510d9508dd947d12e9c6f4b590653ddb77752d26b0e95024e9cf925
ent-DefaultStationBeaconCommand = { ent-DefaultStationBeacon }
    .desc = { ent-DefaultStationBeacon.desc }
    .suffix = Командование
# HASH: 91684c533bdc10dabf6f0ef00c574bb4df035eb88cd3b54a538ab638e5a4cabc
ent-DefaultStationBeaconBridge = { ent-DefaultStationBeaconCommand }
    .desc = { ent-DefaultStationBeaconCommand.desc }
    .suffix = Мостик
# HASH: 7959f0ff7ad38383ea4edae133746a6722365df0af8b6c97583ace47b4895de7
ent-DefaultStationBeaconVault = { ent-DefaultStationBeaconCommand }
    .desc = { ent-DefaultStationBeaconCommand.desc }
    .suffix = Хранилище
# HASH: ad94a67486516326b96a3432e9f238259b85fe4f055492171ad4589b7147e915
ent-DefaultStationBeaconGateway = { ent-DefaultStationBeaconCommand }
    .desc = { ent-DefaultStationBeaconCommand.desc }
    .suffix = Врата
# HASH: cd6a3fd95f62433cec651e1c044ee81c4667a03631113541125f1c419f463a3a
ent-DefaultStationBeaconCaptainsQuarters = { ent-DefaultStationBeaconCommand }
    .desc = { ent-DefaultStationBeaconCommand.desc }
    .suffix = Каюта капитана
# HASH: bf0649c504a965b751cf751896de23ba9144befcab7adf255d76c5f54a199aec
ent-DefaultStationBeaconHOPOffice = { ent-DefaultStationBeaconCommand }
    .desc = { ent-DefaultStationBeaconCommand.desc }
    .suffix = Офис ГП
# HASH: 9e62fd46db7e826f1f0e7416bd2aa2624c47ca6df1d13c7a77f4f88cc9be48c1
ent-DefaultStationBeaconSecurity = { ent-DefaultStationBeacon }
    .desc = { ent-DefaultStationBeacon.desc }
    .suffix = Отдел СБ
# HASH: faf3d11fb847459b0d3b4c8a636a4bc6089eca058b17985014c82a7a2ebd5a26
ent-DefaultStationBeaconBrig = { ent-DefaultStationBeaconSecurity }
    .desc = { ent-DefaultStationBeaconSecurity.desc }
    .suffix = Бриг
# HASH: fbbe5041c1a30ddb0ac855dbcb3e6643cdbb12af048dfcadc3e88ae2a3af60b6
ent-DefaultStationBeaconBrigMed = { ent-DefaultStationBeaconSecurity }
    .desc = { ent-DefaultStationBeaconSecurity.desc }
    .suffix = Бригмед
# HASH: abcfd0d7636b808f69845ef7e27fc278479f0b35a15ea490d58a9652905b8d91
ent-DefaultStationBeaconWardensOffice = { ent-DefaultStationBeaconSecurity }
    .desc = { ent-DefaultStationBeaconSecurity.desc }
    .suffix = Офис смотрителя
# HASH: faeb8644b3bc3919a8de127e0d6a20c91f7f3987dfe2508a84d010652ce7d8cd
ent-DefaultStationBeaconHOSRoom = { ent-DefaultStationBeaconSecurity }
    .desc = { ent-DefaultStationBeaconSecurity.desc }
    .suffix = Комната ГСБ
# HASH: 47dbd2c36e9b8f9fa0ff50742836b18863e41acf289bdb115bc72668d64342a0
ent-DefaultStationBeaconArmory = { ent-DefaultStationBeaconSecurity }
    .desc = { ent-DefaultStationBeaconSecurity.desc }
    .suffix = Оружейная
# HASH: 0600363f3285b33bfa869d4f06370e3e77ae9709dc5f2f1369cb0d3f7484cbce
ent-DefaultStationBeaconPermaBrig = { ent-DefaultStationBeaconSecurity }
    .desc = { ent-DefaultStationBeaconSecurity.desc }
    .suffix = Пермабриг
# HASH: 9eeb780432c6e2faa437bd5852cb84a63291aab5fe296951a6536651b084fe7d
ent-DefaultStationBeaconDetectiveRoom = { ent-DefaultStationBeaconSecurity }
    .desc = { ent-DefaultStationBeaconSecurity.desc }
    .suffix = Комната детектива
# HASH: fd197f75ce160eb9e65762129d9f8d6b2b790d39f7d3cc0ed1afb9b1aa7d5961
ent-DefaultStationBeaconCourtroom = { ent-DefaultStationBeaconSecurity }
    .desc = { ent-DefaultStationBeaconSecurity.desc }
    .suffix = Зал суда
# HASH: 46b1408fcc6f44460674f794a82c4a213d94948867dee45323db34f992a9de10
ent-DefaultStationBeaconLawOffice = { ent-DefaultStationBeaconSecurity }
    .desc = { ent-DefaultStationBeaconSecurity.desc }
    .suffix = Офис АВД
# HASH: 9133bcd793f241c306d6d7206f864ec12d95ef5c580efc9e8998f434b1d74b82
ent-DefaultStationBeaconSecurityCheckpoint = { ent-DefaultStationBeaconSecurity }
    .desc = { ent-DefaultStationBeaconSecurity.desc }
    .suffix = КПП СБ
# HASH: 315ccebe5d702d1023263dd2b556ccb1f854478b91703fb06b1693e7b201b37e
ent-DefaultStationBeaconMedical = { ent-DefaultStationBeacon }
    .desc = { ent-DefaultStationBeacon.desc }
    .suffix = Медицинский отдел
# HASH: 40c64285c6b07fe939b4571398b5ace936284d5b8457066ed9f5ecf8bf10c535
ent-DefaultStationBeaconMedbay = { ent-DefaultStationBeaconMedical }
    .desc = { ent-DefaultStationBeaconMedical.desc }
    .suffix = Медбей
# HASH: 8c41c77ef0a4a44afcb229d4106987f6b201b8cdc8639f8e8cc012ecacafc205
ent-DefaultStationBeaconChemistry = { ent-DefaultStationBeaconMedical }
    .desc = { ent-DefaultStationBeaconMedical.desc }
    .suffix = Химия
# HASH: 2e72bedc76e6c4b3178b3ab7b6d882cc4dbc2274d19c7378a3efa806d542ced8
ent-DefaultStationBeaconCryonics = { ent-DefaultStationBeaconMedical }
    .desc = { ent-DefaultStationBeaconMedical.desc }
    .suffix = Крионика
# HASH: 4f964091087ddaad552188ea6ba4bb29e28d2efbc59325cfb743c4a11986c8d0
ent-DefaultStationBeaconCMORoom = { ent-DefaultStationBeaconMedical }
    .desc = { ent-DefaultStationBeaconMedical.desc }
    .suffix = Комната ГВ
# HASH: 505a56ff3286c167419e5b0488ac002f08ea3073aca5b76a4b52d3e039421611
ent-DefaultStationBeaconMorgue = { ent-DefaultStationBeaconMedical }
    .desc = { ent-DefaultStationBeaconMedical.desc }
    .suffix = Морг
# HASH: a843bb97fe5e4cc11ff4c8fc637d53008c1ee2eecca824059785abf9def32363
ent-DefaultStationBeaconSurgery = { ent-DefaultStationBeaconMedical }
    .desc = { ent-DefaultStationBeaconMedical.desc }
    .suffix = Операционная
# HASH: 35bbfa5c291424be2f910a4dc8051216c6cd7f28fd14b5ba0a728c0b097305b9
ent-DefaultStationBeaconPsychology = { ent-DefaultStationBeaconMedical }
    .desc = { ent-DefaultStationBeaconMedical.desc }
    .suffix = Психолог
# HASH: 96b2112948ce90f8236b907759a4db40ed4d52b45737bd64e11556a0fc5d61b0
ent-DefaultStationBeaconClinic = { ent-DefaultStationBeaconMedical }
    .desc = { ent-DefaultStationBeaconMedical.desc }
    .suffix = Клиника
# HASH: d8d8f19c4db2d838d351526bedcbb26e5431e6937f2b957530167b5ba0c3e422
ent-DefaultStationBeaconScience = { ent-DefaultStationBeacon }
    .desc = { ent-DefaultStationBeacon.desc }
    .suffix = Научный отдел
# HASH: fc6916db63273fb4929e544925639ad6a7f75157911cdf41721af05714d2b7d8
ent-DefaultStationBeaconRND = { ent-DefaultStationBeaconScience }
    .desc = { ent-DefaultStationBeaconScience.desc }
    .suffix = НИО
# HASH: cd88950857a76c71ed96eca924b9d0f6c6f6e347096e0545f6b1d0656602acd8
ent-DefaultStationBeaconServerRoom = { ent-DefaultStationBeaconScience }
    .desc = { ent-DefaultStationBeaconScience.desc }
    .suffix = Сервер исследований
# HASH: 4179a502a6edf8e943e48491c719018834cd83bb7f3f7c7a1bf4c18b8005d15a
ent-DefaultStationBeaconRDRoom = { ent-DefaultStationBeaconScience }
    .desc = { ent-DefaultStationBeaconScience.desc }
    .suffix = Комната НР
# HASH: c4eaf04d364ce4503e8bcdd35d62779de94e9979b6d34d3bc5b753d5d92d915f
ent-DefaultStationBeaconRobotics = { ent-DefaultStationBeaconScience }
    .desc = { ent-DefaultStationBeaconScience.desc }
    .suffix = Робототехника
# HASH: c9a6c668fbc378523a708e2019c38010feb34a4fb68047c79710d8bde6796036
ent-DefaultStationBeaconArtifactLab = { ent-DefaultStationBeaconScience }
    .desc = { ent-DefaultStationBeaconScience.desc }
    .suffix = Артефактная
# HASH: 02b3d73815859e0184a2513215ad5a48549d12073770a3f93e40d1ea55ead487
ent-DefaultStationBeaconAnomalyGenerator = { ent-DefaultStationBeaconScience }
    .desc = { ent-DefaultStationBeaconScience.desc }
    .suffix = Генератор аномалий
# HASH: 2c0ab350078a968f89709d91f389033003f59749c7f3e03663775983d3fdc959
ent-DefaultStationBeaconSupply = { ent-DefaultStationBeacon }
    .desc = { ent-DefaultStationBeacon.desc }
    .suffix = Снабжение
# HASH: 3c2e2869f5c611f1f8e1d59e0cab2291f1db31934659821d4976a7685fab4efd
ent-DefaultStationBeaconCargoReception = { ent-DefaultStationBeaconSupply }
    .desc = { ent-DefaultStationBeaconSupply.desc }
    .suffix = Снабжение, приёмная
# HASH: a5c3c2b0a9d9983198f55ad731b43ac33c9006eeaa27e3cd1c970267930b4ae6
ent-DefaultStationBeaconCargoBay = { ent-DefaultStationBeaconSupply }
    .desc = { ent-DefaultStationBeaconSupply.desc }
    .suffix = Снабжение, док
# HASH: 8d620c3ca1b247c29513a9fff078b929eb5da19ab6f98515a653134d30b9e5cf
ent-DefaultStationBeaconQMRoom = { ent-DefaultStationBeaconSupply }
    .desc = { ent-DefaultStationBeaconSupply.desc }
    .suffix = Комната КМ
# HASH: 8da880e017b48f7f84fa311945bb5eae99df6762a998b7b71b1a4c80784a9ce6
ent-DefaultStationBeaconSalvage = { ent-DefaultStationBeaconSupply }
    .desc = { ent-DefaultStationBeaconSupply.desc }
    .suffix = Утилизаторская
# HASH: 422d14bb93b12b4a28f83f16a6e89bc923dacfaabc9f49675b81cc8412f2013c
ent-DefaultStationBeaconEngineering = { ent-DefaultStationBeacon }
    .desc = { ent-DefaultStationBeacon.desc }
    .suffix = Инженерный отдел
# HASH: 9a943cdc4ae01950dee5418476fa1bf7575468e48a6d4872b2427f5aaf988895
ent-DefaultStationBeaconCERoom = { ent-DefaultStationBeaconEngineering }
    .desc = { ent-DefaultStationBeaconEngineering.desc }
    .suffix = Комната СИ
# HASH: 10b0e38e36855bdbb1d3c118bc401b12b4a69ddb810e26e8c65b84b6cadfaced
ent-DefaultStationBeaconAME = { ent-DefaultStationBeaconEngineering }
    .desc = { ent-DefaultStationBeaconEngineering.desc }
    .suffix = ДАМ
# HASH: 6208167a6277d8ab1057cd938d3a8990c5059e7fd9a653fc146c40c1028e9bd1
ent-DefaultStationBeaconSolars = { ent-DefaultStationBeaconEngineering }
    .desc = { ent-DefaultStationBeaconEngineering.desc }
    .suffix = Солнечные панели
# HASH: b8539ba62c0876aeb81c02a50ebbe2ee24ea9ea6a631c4cd45cbcfe8827e0682
ent-DefaultStationBeaconGravGen = { ent-DefaultStationBeaconEngineering }
    .desc = { ent-DefaultStationBeaconEngineering.desc }
    .suffix = Генератор гравитации
# HASH: cda8658e6666ab1be146f8a98935a0625cd4117e3a365c362603e4f7197ea0ef
ent-DefaultStationBeaconAnchor = { ent-DefaultStationBeaconEngineering }
    .desc = { ent-DefaultStationBeaconEngineering.desc }
    .suffix = Якорь
# HASH: 9d99d5a98f94aaaecb93e23d09778999f0cbc9f8f00011cf3fb688480ccd3950
ent-DefaultStationBeaconSingularity = { ent-DefaultStationBeaconEngineering }
    .desc = { ent-DefaultStationBeaconEngineering.desc }
    .suffix = Контроль УЧ
# HASH: cd467a9de0eb6c98fa344466b5839f196a7dfd598fc4d289cc2161d54e588514
ent-DefaultStationBeaconPowerBank = { ent-DefaultStationBeaconEngineering }
    .desc = { ent-DefaultStationBeaconEngineering.desc }
    .suffix = Энергетический резерв СМЭС
# HASH: 4f1411fc2bc41893724c707f14bde828d56e56e2013ea9e9b5d959164c31c35c
ent-DefaultStationBeaconTelecoms = { ent-DefaultStationBeaconEngineering }
    .desc = { ent-DefaultStationBeaconEngineering.desc }
    .suffix = Телекоммуникации
# HASH: 8167858d484fa4db9feff270d22673fe10eb44f404f385b1c870852b63ba718f
ent-DefaultStationBeaconAtmospherics = { ent-DefaultStationBeaconEngineering }
    .desc = { ent-DefaultStationBeaconEngineering.desc }
    .suffix = Атмосферный отсек
# HASH: d8d44c2ead0ad0e534250d3cbcf2dc19bc5cd7fa9063e0259a0b43b25fc993ed
ent-DefaultStationBeaconTEG = { ent-DefaultStationBeaconEngineering }
    .desc = { ent-DefaultStationBeaconEngineering.desc }
    .suffix = ТЭГ
# HASH: f120ae79db570eba826e8271e6415d94d31dc95cefa6da7cdb20260561cda006
ent-DefaultStationBeaconTechVault = { ent-DefaultStationBeaconEngineering }
    .desc = { ent-DefaultStationBeaconEngineering.desc }
    .suffix = Технологическое хранилище
# HASH: 4505b4602ebb6b780f403fb00fee8f06ea2eabeaa42a89aaf357a963c8456436
ent-DefaultStationBeaconService = { ent-DefaultStationBeacon }
    .desc = { ent-DefaultStationBeacon.desc }
    .suffix = Сервис
# HASH: b4dd940e9b97935744524e3e318c7a3b0f90d429d254ea5eea4f8030c238d850
ent-DefaultStationBeaconKitchen = { ent-DefaultStationBeaconService }
    .desc = { ent-DefaultStationBeaconService.desc }
    .suffix = Кухня
# HASH: 5fe9a83575aaafb774fe24c7cfc0ed8694b5385890d8032748b47e4fa7457ce8
ent-DefaultStationBeaconBar = { ent-DefaultStationBeaconService }
    .desc = { ent-DefaultStationBeaconService.desc }
    .suffix = Бар
# HASH: eaadd9a98611b49ff60e34d7376ee10a89e64b10ff863420d34a4e68e61588c3
ent-DefaultStationBeaconBotany = { ent-DefaultStationBeaconService }
    .desc = { ent-DefaultStationBeaconService.desc }
    .suffix = Гидропоника
# HASH: 642ba634eb47f05895a8e7fb855eac705b1dbcdbed38c61819f464ffed9b208d
ent-DefaultStationBeaconJanitorsCloset = { ent-DefaultStationBeaconService }
    .desc = { ent-DefaultStationBeaconService.desc }
    .suffix = Коморка уборщика
# HASH: be33850868e16102c888b039c3e14d6d3dc19efefe3a0f74485c9deecbbd0da7
ent-DefaultStationBeaconAI = { ent-DefaultStationBeacon }
    .desc = { ent-DefaultStationBeacon.desc }
    .suffix = ИИ
# HASH: 2015e093efd6c41f1f5a60677af3c65e4348700b93d71a7dcc5448ddc57f94e4
ent-DefaultStationBeaconAISatellite = { ent-DefaultStationBeaconAI }
    .desc = { ent-DefaultStationBeaconAI.desc }
    .suffix = Спутник ИИ
# HASH: 54ebd7448e5b479cb085e05f72cc1cd7cfcd1c38719bb9040bb2a6ebea4d5633
ent-DefaultStationBeaconAICore = { ent-DefaultStationBeaconAI }
    .desc = { ent-DefaultStationBeaconAI.desc }
    .suffix = Ядро ИИ
# HASH: 527bf58115895ba7ff550683a3112dcfe293a002173670142e10ee557595b276
ent-DefaultStationBeaconAIUpload = { ent-DefaultStationBeaconAI }
    .desc = { ent-DefaultStationBeaconAI.desc }
    .suffix = Загрузка ИИ
# HASH: 81af794a6a0c20952e4176be1217b36a1302115aae401cfcbb0b12183ea1669b
ent-DefaultStationBeaconAIPower = { ent-DefaultStationBeaconAI }
    .desc = { ent-DefaultStationBeaconAI.desc }
    .suffix = Энергопитание ИИ
# HASH: 6b6104e78ed0bb24c4d2581eb32b9e6a718f469ae6e0508870f5c2f8f4885b9e
ent-DefaultStationBeaconArrivals = { ent-DefaultStationBeacon }
    .desc = { ent-DefaultStationBeacon.desc }
    .suffix = Прибытие
# HASH: e9eb66aabe80b5f468c889e425ada384012b3209471bfd766a3f5f2a7acd32a2
ent-DefaultStationBeaconEvac = { ent-DefaultStationBeacon }
    .desc = { ent-DefaultStationBeacon.desc }
    .suffix = Эвакуация
# HASH: a224761b255538e6b33739918c3ddee023111343f70bea4ba67d23e8fc416271
ent-DefaultStationBeaconDockingArm = { ent-DefaultStationBeacon }
    .desc = { ent-DefaultStationBeacon.desc }
    .suffix = Стыковочная зона
# HASH: cda647ca9428107486cbacc2eff63f495353de68f22480aa224bc312af8925f9
ent-DefaultStationBeaconEVAStorage = { ent-DefaultStationBeacon }
    .desc = { ent-DefaultStationBeacon.desc }
    .suffix = Хранилище EVA
# HASH: 489e0f189f1e17ea556f2fa6edb5e4f895d53db1adb94fa11f45d9bc36b0d23f
ent-DefaultStationBeaconChapel = { ent-DefaultStationBeacon }
    .desc = { ent-DefaultStationBeacon.desc }
    .suffix = Церковь
# HASH: 6cef3adc5324fc6ac8c5bacaacdb705b1d27e42a927423253d029c7429d982d1
ent-DefaultStationBeaconLibrary = { ent-DefaultStationBeacon }
    .desc = { ent-DefaultStationBeacon.desc }
    .suffix = Библиотека
# HASH: ed22701f319b42330a7aebf6a69880ef7bb18af5e22d4dbc101ac6b9604c7b78
ent-DefaultStationBeaconReporter = { ent-DefaultStationBeacon }
    .desc = { ent-DefaultStationBeacon.desc }
    .suffix = Репортёр
# HASH: cd7af90c4aa41eedd54aef579997c1676c95aa7fab53b08425c4d1307051e2a9
ent-DefaultStationBeaconTheater = { ent-DefaultStationBeacon }
    .desc = { ent-DefaultStationBeacon.desc }
    .suffix = Театр
# HASH: d4bb6ef63b7488e58ab1a2a225c7a60186142cf642f2c5f719f749beb3fe109c
ent-DefaultStationBeaconDorms = { ent-DefaultStationBeacon }
    .desc = { ent-DefaultStationBeacon.desc }
    .suffix = Жилые помещения
# HASH: 6a4c651edeb35a13b8b0faaf8959be437a96432d5bf017f70938b41b05e042b5
ent-DefaultStationBeaconToolRoom = { ent-DefaultStationBeacon }
    .desc = { ent-DefaultStationBeacon.desc }
    .suffix = Хранилище инструментов
# HASH: 8d9ff4adf37032a437819aa31a94afb628f96f822825528f083bd6b35e24b90f
ent-DefaultStationBeaconDisposals = { ent-DefaultStationBeacon }
    .desc = { ent-DefaultStationBeacon.desc }
    .suffix = Мусоросброс
# HASH: a49e4e26661c2aaae23e72b0a199349c326fc506b6769542a1ef57315b74b861
ent-DefaultStationBeaconCryosleep = { ent-DefaultStationBeacon }
    .desc = { ent-DefaultStationBeacon.desc }
    .suffix = Криосон
# HASH: b8da1a83fc72d5df0e997a3714866d4361b768eed2a2f501b314708b69476833
ent-DefaultStationBeaconEscapePod = { ent-DefaultStationBeacon }
    .desc = { ent-DefaultStationBeacon.desc }
    .suffix = Спасательная капсула
# HASH: 8c01ea2ee250d1bcc67e488c2311682ba747df0130bfd515cb57f692e259987d
ent-DefaultStationBeaconVox = { ent-DefaultStationBeacon }
    .desc = { ent-DefaultStationBeacon.desc }
    .suffix = Вокс
