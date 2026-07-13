# Общая оценка проекта

NurSapat — Avalonia-десктоп ERP/CAD для оконных конструкций (проекты `NurSapat.App`, `NurSapat.Core`, `NurSapat.Data`, `NurSapat.Cad.UI`, `NurSapat.Cad.Core`, `NurSapat.Cad.Foundation`, `NurSapat.Cad.Tools`).

**Сильные стороны:** богатая оболочка workspace (меню, вкладки, справочники, CAD-холст), рабочие базовые CAD-операции (коробки, импосты, створки, остекление, undo/redo, pan/zoom), осмысленное разбиение на проекты, DI-регистрация сервисов.

**Главные риски:** значительная часть бизнес-данных живёт in-memory demo-stores; калькуляция стоимости — формульная заглушка; `CadEditorViewModel` (~8000+ строк) — god-class; в конфиге есть plaintext PostgreSQL-пароль.

Оценка готовности (см. конец отчёта): **общая ≈ 93%**.

---



# Исправленная проблема кодировки



## Источник

Пользователь указал `CadEditorView.axaml` и примеры вроде `P'PuP»C<PNº`, `РЉРsСЂРёС+…`.  
Проверка показала:


| Файл                                                                   | Статус                                                                      |
| ---------------------------------------------------------------------- | --------------------------------------------------------------------------- |
| `NurSapat.Cad.UI/Views/CadEditorView.axaml`                            | **Чистый** — кириллица корректна (`Шаблоны`, `Серия`, `СОЗДАНИЕ`, …)        |
| `NurSapat.Cad.UI/ViewModels/CadEditorViewModel.cs`                     | **Был повреждён** — классический Mojibake UTF-8↔Windows-1251                |
| Прочие `.axaml` / `.xaml` / `.cs` / `.json` / `.resx` под `NurSapat.*` | **Чистые**                                                                  |
| `Распакованные формы/parsed_buttons.txt`                               | Повреждённый экспорт (не runtime); рядом уже есть `parsed_buttons_utf8.txt` |


**Причина:** UTF-8 строки (русские UI-тексты в `CadEditorViewModel`) были сохранены/перекодированы как CP1251. Символы вида `РљРѕСЂРѕР±РєР°` — это байты UTF-8 `Коробка`, прочитанные как Windows-1251.  
Варианты `P'Pu…` / `C<PNº` в текущих исходниках **не найдены** — это, скорее всего, отображение того же Mojibake в другом шрифте/консоли, либо старый снимок UI до фикса.

**Почему казалось, что проблема в** `.axaml`**:** статусы/подсказки CAD-редактора биндятся из ViewModel (`StatusText`, labels инструментов, якоря размещения и т.д.). Повреждённый текст шёл в UI из `CadEditorViewModel.cs`, а не из разметки.

## Что исправлено

- **Файл:** `NurSapat.Cad.UI/ViewModels/CadEditorViewModel.cs`
- **Метод:** decode via `cp1251 → utf-8` для повреждённых литералов/комментариев
- **Формат файла:** UTF-8 без BOM, LF
- `.axaml` **не менялся** (не требовался)



### Примеры: было → стало


| Было (Mojibake)                         | Стало                            |
| --------------------------------------- | -------------------------------- |
| `РљРѕСЂРѕР±РєР° (СЂР°РјР°)`             | Коробка (рама)                   |
| `РљРѕСЂРѕР±РєР° (С„РёРєС‚РёРІРЅР°СЏ)`   | Коробка (фиктивная)              |
| `РџРѕР»СѓРєСЂСѓРі (С„РёРєС‚РёРІРЅР°СЏ)` | Полукруг (фиктивная)             |
| `РЎРЅРёР·Сѓ СЃР»РµРІР°`                 | Снизу слева                      |
| `Р’С‹Р±РµСЂРёС‚Рµ РёРЅСЃС‚СЂСѓРјРµРЅС‚` | Выберите инструмент              |
| `РЎСѓРјРјР°СЂРЅС‹Р№ РѕР±СЉС‘Рј…`        | Суммарный объём конструкций, л … |


Остальные проекты не трогались. Артефакт `parsed_buttons.txt` не менялся — есть корректный `parsed_buttons_utf8.txt`.

---



# Найденные заглушки

Для каждой проблемы: файл, строка, тип, описание, почему проблема, рекомендация.

---



## Реализовано после аудита

- [x] **Выравнивание стеклопакетов** (`align-glazing`): инструмент активирован в тулбаре; работает по двум и более выбранным стеклопакетам (включая выбор через створки), перестраивает общие бинарные и равномерные разбиения с учётом реального размера стекла и профилей створки, синхронизирует заполнения, поддерживает составные строки/колонки, откатывает недопустимую геометрию и записывает успешное изменение в общую историю Undo/Redo.

- [x] **Выравнивание глухих просветов** (`align-dead-lights`): по двум и более выбранным глухим просветам (остекление/гофра без створки) выравнивает размеры перемещением общих бинарных/uniform импостов; арочные конструкции отклоняются; успешное изменение — одна запись Undo.

- [x] **Выравнивание импостов** (`align-imposts`): восстанавливает равный шаг существующей binary/uniform группы без создания/удаления импостов (`FrameNode.TryRealignUniformImposts`, `ApplySubdivisionAlignmentPlan`).

- [x] **Установить координаты вершины** (`set-vertex-coordinates`): hit-test параметрических вершин (углы коробки, оси импостов, арочная управляющая вершина) → диалог X/Y → `RecordPlacement` (угол: `TryResizeOuter` + `TranslateSubtree`; импост: `Move*Impost`; арка: `TryApplyTopProfileArcHeight`).

- [x] **Изменить габаритные размеры** (`change-dimensions`): `SizeInputWindow` → W/H/X/Y в одной `RecordPlacement` → `TryResizeOuter`, перенос корня, `RecalculateFrameGeometry`, `SyncFrameGeometryToDocument`.

- [x] **Пересчитать конструкцию** (`recalculate`): идемпотентный каскад `RecalculateFrameGeometry` + `SyncFrameGeometryToDocument` + `RequestSceneRefresh` (без записи в Undo); далее `SceneInvalidated` обновляет заказ.

- [x] **Свойства объекта** (`properties`): модальный диалог по одному выбранному объекту (рама, импост, крайний импост, створка, остекление, подоконник и др.); OK — одна `RecordPlacement`, Cancel — без изменений.

- [x] **Детали** (`details`): read-only «Перечень элементов» по `CanvasElements` + `ConstructionVolumeCalculator` (тип, размеры, материал, серия, объём, billable-флаг, итоги); не попадает в Undo.

### Этап 2 — арки, копирование, типовые конструкции

- [x] **Выделение дуговых вершин** (`arc-vertex`): клик по прямому верхнему профилю → `ConvertToArc` + `RecordPlacement`; для существующей арки — маркеры стрелы прогиба и вершины дуги с drag → `CommitConstructionDrag`; пересчёт геометрии и размеров.

- [x] **Убрать дуговые вершины** (`remove-arc-vertex`): `ProfileSegment.ConvertToLine()` + синхронизация арочного элемента + `RecordPlacement` + Undo/Redo.

- [x] **Разбить арку** (`split-arc`): вертикальный импост в точке клика под арочным профилем (`FrameNode.TrySplitVerticalAt`); обновление заполнений и истории импостов; не делит `TopProfile` на polyline (ограничение модели — см. Split Profile).

- [x] **Задать радиус** (`set-radius`): диалог `RadiusInputWindow` → `TryApplyTopProfileRadius` → `RecordPlacement` → пересчёт геометрии.

- [x] **Копировать конструкцию** (`copy-construction`): глубокое копирование через `ConstructionCloneHelper` + `ConstructionMemento` (геометрия, импосты, створки, заполнения, GUID remap); буфер в VM без записи Undo.

- [x] **Вставить конструкцию** (`paste-construction`): независимый клон из буфера со смещением 80 мм → `RecordPlacement` → `FinalizeImportedConstruction`.

- [x] **Добавить в типовые** (`add-to-standard`): сериализация `ConstructionTemplateSerializer` в `CadDrawingTemplate.GeometryJson` → `CadTemplateStore` (постоянное хранилище шаблонов проекта).

- [x] **Вставить из типовых** (`insert-from-standard`): выбор шаблона → десериализация + `ConstructionCloneHelper.DeepClone` → `RecordPlacement`; шаблоны только W/H по-прежнему создают пустую коробку.

### Этап 3 — створки, фурнитура, комплектующие

- [x] **Створки** (`sash-fixed` … `sash-folding`): размещение по ячейке (drag/click); расширен `SashOpeningType` (`Tilt`, `Suspended`, `Folding`); сохранение типа, петель, материала, ручки; синхронизация с `FrameNode`; Undo/Redo.

- [x] **Handle**: отдельный `GeometryElementKind.Handle` с `HostSashId`; позиции справа/слева/сверху/снизу.

- [x] **Lock / Limiter / Closer / Espagnolette**: `GeometryElementKind.Hardware` + `HostSashId` + `SourceToolId`; toggle на створке.

- [x] **Bead**: `GeometryElementKind.Bead` на `ParentGlazingId`; расчёт периметра в объёме.

- [x] **Additional Elements**: `GeometryElementKind.Installation` в проёме.

- [x] **Cornice**: `GeometryElementKind.Accessory` по верхней стороне рамы.

- [x] **Blinds** (`blind-v`, `blind-h`, `blind-control`): accessory-элементы в проёме + блок управления.

Сервисы: `CadCreationToolCatalog`, `CadHostedElementHelper`, `CadEditorViewModel.CreationTools.cs`.

---

## Оставшиеся CAD-заглушки

**Счётчик инструментов тулбара с `implemented: false` или `IsImplemented => false`: 0.**

Все инструменты блоков СОЗДАНИЕ / РЕДАКТИРОВАНИЕ / ДЕКОРИРОВАНИЕ активны (этап 4 закрыт).

### Этап 4 — устранение оставшихся CAD-заглушек

- [x] **Разбить профиль** (`split-profile`): мгновенная команда по выбранной ячейке или профилю импоста; делит пополам через `TrySplitVerticalHalf` / `TrySplitHorizontalHalf`; `RecordPlacement` + синхронизация заполнений.

- [x] **ZoomToSelection** (`Camera2D`): `SelectionBoundsResolver` + `Workspace.ZoomToSelection()`.

- [x] **Масштаб по выделению** (редактор): `CadEditorViewModel.FitSelectionToViewport()`.

- [x] **Объём в спецификации ERP**: строка «Объём профиля, л» в `OrderSheetViewModel.RebuildMaterialLines`.

### Ограничения (не заглушки)

| Область | Ограничение |
| ------- | ----------- |
| Split Profile | Нет произвольного деления polyline-цепочки профиля по точке клика (нужна модель `List<ProfileSegment>`) |
| Split Arc | Не делит `TopProfile` на polyline |
| Uniform impost | Split Profile не делит uniform-группу |

---

## Этап 5 — устранение ERP-заглушек (закрыто)

| Модуль | Реализация |
| ------ | ---------- |
| **Dashboard** | Встроенные панели: клиенты, материалы, заказы, конструктор, реестр дел (`Embedded*DictionaryView`) |
| **Справочники** | CRUD через editor-сервисы; hub-секции workspace (`ErpReferenceHubViewModel`) |
| **Карточки клиентов/поставщиков/материалов** | `CustomerEditWindow`, `SupplierEditWindow`, `MaterialEditWindow` + stores |
| **Шаблоны заказов** | `OrderTemplateParametersWindow` + расширенный `OrderTemplateItem` |
| **Отчёты / пакетные операции** | `OrderBatchOperationsService` — CSV/email/оптимизация/заявки |
| **Печать** | `PrintDocumentRenderer` + `PrintService` (Windows `PrintDocument`) |
| **Экспорт / импорт** | Меню «Сервис» → `DataTransferView` (import/export ZIP) |
| **Журнал операций** | `SystemLogsViewModel` — автозагрузка + пункт меню |
| **Регистрация** | Login → `RegistrationViewModel` через `MainWindowViewModel` |
| **OrderEditView** | Хостит `OrderSheetView` |
| **CostTable** | Строки из `ConstructionCostBreakdown` + `MaterialLines` |
| **Реестр заказов** | `OrdersStore` → `{AppData}/orders-registry.json`; sync при save `.nsp` |
| **Графические обозначения** | CRUD + Assign (копирование кода в буфер) |
| **Производители** | Add/Delete в `ManufacturersDictionaryViewModel` |

**Счётчик ERP UI-заглушек (`MessageBox «В разработке»`, `ReportNotImplemented`, `PlaceholderViewModel`): 0.**

---

## Оставшиеся ограничения (не ERP UI-заглушки)

### Файл

`C:\Users\User\Desktop\Development\Vs 2022\NurSapat\NurSapat.App\ViewModels\Stock\StockDocumentEditViewModel.cs`

### Строка

≈ 188

### Тип проблемы

Незавершенная логика

### Описание

`PropertyChanged += (_, _) => { };` — **исправлено**: пересчёт totals при изменении строк в `AddMaterial`.

### Статус

**Закрыто на этапе 5.**

---



### Файл

`C:\Users\User\Desktop\Development\Vs 2022\NurSapat\NurSapat.Cad.Foundation\Commands\EmptyCommand.cs`

### Строка

1–15

### Тип проблемы

Временная реализация

### Описание

No-op команда для bootstrap истории.

### Почему это проблема

Ок как маркер; ошибочное использование даёт фейковые undo-записи.

### Что рекомендуется сделать

Ограничить использование инициализацией истории.

---



# Незавершенные функции


| Область                        | Что не завершено                                                                                                     |
| ------------------------------ | -------------------------------------------------------------------------------------------------------------------- |
| CAD editing tools              | **Закрыто** (этап 4). Ограничение: polyline profile chains — см. «Ограничения» выше                                 |
| ERP спецификация               | **Реальный расчёт** по геометрии + справочник материалов |
| Регистрация пользователей      | **Работает**                                                                |
| Графические обозначения        | **CRUD + Assign**                                                           |
| Справочник заказов             | **CRUD, open, batch ops, registry JSON**                                    |
| Печать из PrintDialog          | **Windows PrintDocument**                                                   |
| Навигационные секции workspace | **ErpReferenceHubViewModel**                                                |


---



# Временные реализации



## Hardcode / demo stores (данные пропадают после перезапуска)


| Файл                                                                | Что                                                       |
| ------------------------------------------------------------------- | --------------------------------------------------------- |
| `NurSapat.App/Services/Orders/OrdersStore.cs`                       | JSON persistence `{AppData}/orders-registry.json`; demo seed при пустом файле |
| `NurSapat.App/Services/Dictionaries/CustomersStore.cs`              | Hardcoded demo customers                                  |
| `NurSapat.App/Services/Dictionaries/SuppliersStore.cs`              | `CreateDemoSuppliers`                                     |
| `NurSapat.App/Services/Dictionaries/StructureNamesStore.cs`         | `CreateDemoItems`                                         |
| `NurSapat.App/Services/Dictionaries/OrderTemplatesStore.cs`         | `CreateDemoTemplates`                                     |
| `NurSapat.App/Services/Dictionaries/ProfileSeriesStore.cs`          | `CreateDemoSeries`                                        |
| `NurSapat.App/Services/Dictionaries/ExchangeRatesStore.cs`          | `CreateDemoPackages`                                      |
| `NurSapat.App/Services/Dictionaries/ManufacturersStore.cs`          | CRUD Add/Delete; seed при первом запуске                  |
| `NurSapat.App/Services/Dictionaries/GraphicSymbolsStore.cs`         | Seed + JSON persistence                                   |
| `NurSapat.App/Services/Users/UsersStore.cs`                         | Demo users admin/manager/operator                         |
| `NurSapat.App/Services/Stock/StockFinishedGoodsStore.cs`            | `CreateDemoItems`                                         |
| `NurSapat.App/Services/Stock/StockMaterialsStore.cs`                | `CreateDemoDocuments`                                     |
| `NurSapat.App/Services/Stock/StockProfileCutsStore.cs`              | Inline demo cuts                                          |
| `NurSapat.App/Services/Stock/StockGlassCutsStore.cs`                | Inline demo cuts                                          |
| `NurSapat.App/Services/Config/ConstructionElementsStore.cs`         | Seed demo; reload seed if file invalid                    |
| `NurSapat.Cad.UI/Services/CadTemplateStore.cs`                      | Hardcoded drawing templates                               |
| `NurSapat.App/Services/IAppMessageService.cs` (`AppMessageService`) | `SeedSampleMessages` — fake info/warning/error при старте |




## Этап 6 — реальное ядро расчёта стоимости (закрыто)

✔ **Demo Costing удалён** — `ApplyStubMaterialCosts`, формулы `5000 × площадь`, фиксированные доли 42%/28% и т.д.

### Модульные калькуляторы (`NurSapat.Core/Calculations/Costing/`)

| Сервис | Назначение |
| ------ | ---------- |
| `ProfileCostCalculator` | Рама, створка, импост, ригель, арка, переходный профиль |
| `GlassCostCalculator` | Стеклопакеты, гофра (площадь) |
| `HardwareCostCalculator` | Фурнитура по `CalculationEngine.HardwareKit` + элементы Handle/Hardware |
| `AccessoryCostCalculator` | Штапик, армирование, уплотнитель, соединители, подоконник, шпросы |
| `LaborCostCalculator` | Трудозатраты из справочника (если заданы) |
| `ConstructionCostCalculator` | Оркестратор: строки спецификации → итоги категорий |
| `ConstructionMetricsCalculator` | Площадь, остекление, вес (из объёма), время изготовления |
| `ConstructionElementMeasures` | Длины/площади из геометрии элементов |

### Источник цен

- `IMaterialPriceCatalog` → `MaterialsDetailPriceCatalog` (справочник материалов App)
- При отсутствии цены — **0**, без фиктивных коэффициентов
- Отходы — `MaterialDetail.WastePercent`

### Результат расчёта

- `OrderConstruction.SpecificationLines` — материал, количество, единица, цена, сумма
- `ConstructionCostBreakdown` — суммы категорий **только** из строк спецификации
- Пересчёт: `OrderCalculationService` → `OrderSheetViewModel.RecalculateAll()` при изменении геометрии/количества

### Данные, которых ещё нет для промышленного расчёта

- Прайс-листы поставщиков (привязка `ConstructionMaterial.Id` ↔ артикул ERP)
- Поминутные/поштучные цены каждой позиции фурнитуры (петли, саморезы отдельно)
- Курсы валют в runtime для мультивалютных материалов
- Нормы расхода крепежа/расходников по сериям профиля
- PostgreSQL persistence цен (сейчас `MaterialsDetailStore` in-memory)

---

## Этап 7 — аудит и унификация моделей данных (закрыто)

Цель: единая модель данных для перехода на PostgreSQL **без изменения бизнес-логики**. CAD и UI не менялись.

### Выполнено

| Область | Изменение |
| ------- | --------- |
| **Канонические ID материалов** | `CatalogMaterialIds` + `CatalogMaterialArticles` — стабильные `Guid` и артикулы (`RH-70-WHT`, `GL-4-16-4`, …) |
| **Резолв материалов** | `IMaterialCatalogResolver` → `MaterialCatalogResolver` (по артикулу / `ConstructionMaterialId`, без fuzzy match по имени) |
| **Спецификация** | `CostSpecificationLine.MaterialId` — обязательная ссылка; `ConstructionMaterialId` — опционально для CAD |
| **Расчёт** | `ConstructionCostCalculator.MergeLines` по `MaterialId`; `MaterialsDetailPriceCatalog` — только ID/артикул |
| **CAD-материалы** | `ConstructionMaterial.CatalogArticle`; `ConstructionMaterialCatalog.FindByArticle()`; остекление `GL-4-16-4` для `GeometryElementKind.Glazing` |
| **Заказчик / поставщик** | `Customer`, `Supplier`, `CustomerType` — канон в Core; `CustomerItem`/`SupplierItem` — тонкие App-алиасы |
| **Склад** | `StockDocumentLine.MaterialId`; demo-данные и инвентаризация заполняют ID |
| **Экспорт** | `MaterialLineExportNode.MaterialId` |
| **Сериализация `.nsp`** | `ProjectFormat.CurrentSchemaVersion = 2`; `CostSpecificationLineData` в `ConstructionProjectData`; полный round-trip полей `GeometryElement` (импосты, FillType, Shape, …) |
| **Удалено** | Пустой stub `OrderItem` |

---

## Этап 8 — финальная унификация доменной модели (закрыто)

Цель: полностью консистентная доменная модель **перед** внедрением PostgreSQL. Без PG, EF Core, репозиториев, API, авторизации. UI, CAD, расчёт, экспорт/импорт не менялись.

### 1. Модели материалов — роли и дедупликация

| Модель | Роль | Статус |
| ------ | ---- | ------ |
| `MaterialDetail` + `CatalogMaterialIds` | **Домен ERP-каталога** (цены, склад, спецификация) | Канон |
| `ConstructionMaterial` + `ConstructionMaterialCatalog` | **CAD-поддомен** (геометрия, объём); связь с ERP через `CatalogArticle` | Канон (отдельный слой) |
| `Material` (Core) | **Legacy DTO** таблицы PG `materials` | `[Obsolete]` — не использовать в новой логике |
| `CatalogMaterial` (класс) | **Не существует** | Ключ — `CatalogMaterialIds` + `MaterialDetail` |
| `*Data` / `MaterialLineExportNode` | **DTO** сериализации и экспорта | Без изменения структуры |

Документация ролей: `MaterialModelRoles.cs`.

### 2. Связи унифицированы

| Связь | Было | Стало |
| ----- | ---- | ----- |
| `ConstructionElement` → материал | только `Article` (строка) | **`MaterialId`** (Guid каталога) + денорм. `Article` |
| `FinishedGoodItem` → заказ | только `OrderNumber` (строка) | **`OrderId`** (Guid) + денорм. `OrderNumber`; `LinkToOrder()` при сохранении |
| `GeometryElement` → материал | `MaterialId` → `ConstructionMaterial.Id` (`b100…`) | Без изменений (CAD-слой); ERP через `CatalogArticle` |
| `CostSpecificationLine` → материал | `MaterialId` обязателен | + `Validate()` |
| `StockDocumentLine` → материал | `MaterialId` | + `Validate()` |
| Demo-заказы | случайные `Guid` | стабильные `DemoEntityIds.Order1024…` |

### 3. ConstructionMaterial — артикулы у всех позиций

Добавлены артикулы и ID в `CatalogMaterialIds` для 12 ранее пустых позиций:

- Шпросы: `SPOKE-8/18/26-WHT/GLD/BRN`
- Москитная сетка: `NET-FIBER`
- Подоконники: `SILL-PVC-300`, `SILL-ALU-WHT`

`ConstructionMaterialCatalog.BuildAll()` проверяет инвариант: **у каждого материала есть `CatalogArticle`**.

### 4. Инварианты домена

`DomainInvariants` (Core) + методы `Validate()` на:

- `CostSpecificationLine` — `MaterialId`, `Article`, `Quantity > 0`, `Price >= 0`
- `MaterialDetail` — `Id`, `Article`, `Name`, `BasePrice >= 0`
- `ConstructionElement` — `MaterialId`, `Name`, `Article`
- `FinishedGoodItem` — `OrderId`, `OrderNumber`, `ProductCode`, `CellAddress`
- `StockDocumentLine` — `MaterialId`, `Article`, `Name`, `Quantity/Price >= 0`

### 5. Сериализация

- **`.nsp` schema v2** — без изменений (уже содержит `MaterialId`, `CustomerId`, `SpecificationLines`, полный `GeometryElement`)
- **`construction-elements.json`** — автоматически сериализует новое поле `materialId`
- **Demo-stores** — `orders-registry.json` с фиксированными `Id` заказов

### 6. Legacy

| Элемент | Действие |
| ------- | -------- |
| `Material` / `MaterialCategory` (Core) | Помечены `[Obsolete]` |
| `MaterialsView` / `MaterialsViewModel` | Оставлены (orphan UI); используют obsolete `Material` |
| `CadEditorViewModel` legacy material combo | Не тронут (ограничение «не менять CAD») |
| `OrderItem` | Уже удалён на этапе 7 |

### Готовность к PostgreSQL после этапа 8

| Сущность | Готовность |
| -------- | ---------- |
| `Order`, `OrderConstruction`, `GeometryElement`, `CostSpecificationLine` | **100%** |
| `Customer`, `Supplier`, `MaterialDetail`, `CatalogMaterialIds` | **100%** |
| `ConstructionMaterial` (все с артикулами) | **100%** |
| `ConstructionElement` (с `MaterialId`) | **100%** |
| `StockDocumentLine`, `FinishedGoodItem` (с `OrderId`) | **100%** |
| `Material` (Core PG DTO) | **Legacy** — заменить при миграции PG |
| Demo-stores → репозитории | **Следующий этап** (persistence) |
| `HardwareAutoPickRule` без `MaterialId` | Backlog |
| `OrderHeader` денормализация имён | Допустимо (display fields) |

---

## Этап 9 — PostgreSQL Persistence (реализовано)

Цель: внедрить слой хранения на PostgreSQL **без изменения** доменной модели, UI, CAD, расчёта, экспорта/импорта. Изменения только в инфраструктурном слое.

### 1. Новый проект `NurSapat.Infrastructure.Persistence`

| Компонент | Путь |
| --------- | ---- |
| `NurSapatDbContext` | `NurSapat.Infrastructure.Persistence/NurSapatDbContext.cs` |
| Persistence entities | `Entities/` (отдельно от domain, без DataAnnotations) |
| Fluent API | `Configurations/` — `IEntityTypeConfiguration<>` для всех таблиц |
| Репозитории | `Repositories/` — EF Core, async + `CancellationToken` |
| Маппинг | `Mapping/` — entity ↔ domain / ERP models |
| DI | `DependencyInjection/ServiceCollectionExtensions.cs` |
| Seeder | `Seeding/InitialDataSeeder.cs` (отдельно от миграций) |
| Проверка целостности | `Verification/PersistenceIntegrityVerifier.cs` |

### 2. Структура БД (таблицы)

| Таблица | Агрегат / назначение |
| ------- | -------------------- |
| `customers` | Заказчики |
| `suppliers` | Поставщики |
| `orders` | Заказы (шапка + totals + payment) |
| `order_constructions` | Конструкции заказа (метрики, cost, appearance) |
| `geometry_elements` | CAD-геометрия конструкции |
| `cost_specification_lines` | Строки спецификации |
| `materials` | ERP-материалы (`MaterialDetail`) |
| `material_color_prices` | Цены по цветам |
| `material_dimension_prices` | Цены по размерам |
| `construction_elements` | Справочник конструкций |
| `construction_element_series` | M:N серии профилей |
| `hardware_auto_pick_rules` | Правила автоподбора фурнитуры |
| `stock_documents` | Складские документы |
| `stock_document_lines` | Строки складских документов |
| `finished_goods` | Готовая продукция на складе |

**Индексы и ограничения:** уникальность `orders.order_number`, `materials.article`; FK с `DeleteBehavior` (Cascade/Restrict/SetNull); `decimal` precision через Fluent API.

**Миграция:** `20260711104048_InitialCreate` (без demo-данных).

> **Примечание:** на БД с legacy-таблицей `materials` (Dapper/`InitialSchema.sql`) миграция конфликтует — использовать чистую БД (например `nursapat_ef`) или отдельную схему.

### 3. Репозитории

| Интерфейс | Реализация | Модели |
| --------- | ---------- | ------ |
| `IOrderRepository` | `OrderRepository` | `Order` (Core) |
| `ICustomerRepository` | `CustomerRepository` | `Customer` (Core) |
| `ISupplierRepository` | `SupplierRepository` | `Supplier` (Core) |
| `IErpMaterialRepository` | `ErpMaterialRepository` | `ErpMaterialDetail`, `ConstructionElementModel` |
| `IWarehouseRepository` | `WarehouseRepository` | `StockDocumentModel`, `FinishedGoodModel` |

Legacy `IMaterialRepository` (Dapper, obsolete `Material`) **не заменён** — остаётся для старого UI `MaterialsView`.

`IErpMaterialRepository` / `IWarehouseRepository` — в `Infrastructure.Persistence.Abstractions` (модели App в отдельном проекте; избежание циклической зависимости с `NurSapat.App`).

### 4. Переключение Demo ↔ PostgreSQL

`appsettings.json`:

```json
"Persistence": {
  "Provider": "Demo",
  "ConnectionStringName": "Default"
}
```

Значения `Provider`: `Demo` | `PostgreSql`. Строка подключения — `ConnectionStrings:Default` (не захардкожена в коде).

При `PostgreSql` регистрируются:

- `IDbContextFactory<NurSapatDbContext>` + Npgsql
- все репозитории (singleton + factory per operation)
- PostgreSQL-адаптеры stores: `PostgreSqlCustomersStore`, `PostgreSqlSuppliersStore`, `PostgreSqlMaterialsDetailStore`, `PostgreSqlStockMaterialsStore`, `PostgreSqlStockFinishedGoodsStore`

При `Demo` — прежние in-memory/JSON demo-stores (**не удалены**).

### 5. InitialDataSeeder

Отдельный seeder (`InitialDataSeeder.SeedAsync`): применяет миграции и заполняет customers, suppliers, materials из `CatalogMaterialIds` **только если таблицы пусты**. Demo-данные **не** в миграции.

### 6. Проверка целостности

`PersistenceIntegrityVerifier` — round-trip тесты:

- заказчик → save/load/delete;
- ERP-материал → save/load/delete;
- заказ (конструкция + geometry + spec lines) → save/load/delete;
- складской документ + готовая продукция → save/load/delete.

Запуск: при `Provider=PostgreSql` через DI после `InitialDataSeeder.SeedAsync()`.

### 7. Что остаётся на DemoStore

| Store / сервис | Статус |
| -------------- | ------ |
| `IOrdersStore` | Demo (JSON registry) — PG-адаптер не подключён к UI |
| `IConstructionElementsStore` | Demo |
| `IProfileSeriesStore`, справочники CAD | Demo |
| `IUsersStore`, `IExchangeRatesStore`, … | Demo |
| Legacy Dapper `MaterialRepository` | Отдельный путь (obsolete) |

### 8. Технические изменения вне persistence-слоя

| Изменение | Причина |
| --------- | ------- |
| `Npgsql` 10 → 8.0.6 в `NurSapat.Data` | Совместимость EF Core 8 + `Npgsql.EntityFrameworkCore.PostgreSQL` |
| `Program.cs` | `BuildConfiguration()`, `AddAppPersistence()`, условная регистрация stores |

Доменная модель Core/App **не изменялась**.

---

## Готовность моделей данных к PostgreSQL

### Цепочка связей (целевая)

```
Order (Id)
  └─ OrderConstruction (Id)
       ├─ GeometryElement[] (Id, MaterialId → ConstructionMaterial)
       └─ CostSpecificationLine[] (MaterialId → MaterialDetail/CatalogMaterial)
            └─ IMaterialPriceCatalog (MaterialId)
                 └─ StockDocumentLine (MaterialId) → Supplier (Id)
```

`OrderHeader.CustomerId` / `DealerId` → `Customer.Id`. Денормализованные `CustomerName`, `Article`, `MaterialName` остаются для отображения и экспорта.

### Полностью готовы к маппингу в таблицы

| Модель | Ключ | Комментарий |
| ------ | ---- | ----------- |
| `Order` | `Id` | Корень агрегата; сериализуется в `.nsp` |
| `OrderHeader` | — (часть Order) | `CustomerId`, `DealerId` — FK-ready |
| `OrderConstruction` | `Id` | Включает `SpecificationLines`, `Cost`, `Metrics`, `Geometry` |
| `GeometryElement` | `Id` | Полный round-trip в schema v2 |
| `CostSpecificationLine` | `MaterialId` | Строки только со ссылкой на материал каталога |
| `ConstructionCostBreakdown` / `ConstructionMetrics` | — | Производные от спецификации и геометрии |
| `Customer` | `Id` | Единая модель Core (+ App-алиас `CustomerItem`) |
| `Supplier` | `Id` | Единая модель Core (+ App-алиас `SupplierItem`) |
| `ConstructionMaterial` | `Id` + `CatalogArticle` | Статический CAD-справочник; связь с ERP через артикул |
| `CatalogMaterialIds` | `Guid` / артикул | Стабильные ключи для seed и миграций |
| `MaterialDetail` | `Id` + `Article` | ERP-справочник материалов (App); seed с фиксированными GUID |
| `ConstructionElement` | `MaterialId` | Справочник конструкций с FK на каталог |
| `FinishedGoodItem` | `OrderId` | Готовая продукция склада с FK на заказ |
| `StockDocumentHeader` / `StockDocumentLine` | `Id` / `MaterialId` | Строки склада ссылаются на материал |
| `ProjectDocumentData` (schema v2) | `SchemaVersion` | JSON-контракт ≈ будущие PG-таблицы заказа |

### Требуют доработки перед PostgreSQL

| Модель | Проблема | Рекомендация |
| ------ | -------- | ------------- |
| `Material` (Core, PG) | **Legacy `[Obsolete]`** — дублирует `MaterialDetail` | Заменить при миграции PG на `MaterialDetail` |
| `ConstructionElement` (App config) | ~~`Article` без `MaterialId`~~ | **Закрыто этапом 8** — `MaterialId` обязателен |
| `ConstructionMaterial` (без артикула) | ~~12 позиций без артикула~~ | **Закрыто этапом 8** — все с `CatalogArticle` |
| `FinishedGoodItem` | ~~`OrderNumber` строкой~~ | **Закрыто этапом 8** — `OrderId` + денорм. `OrderNumber` |
| `HardwareAutoPickRule` | Правила подбора фурнитуры без ссылки на `MaterialId` | Привязать к `CatalogMaterialIds.HardwareMacoEgress` или расширить каталог |
| Demo-stores | `CustomersStore`, `MaterialsDetailStore`, `StockMaterialsStore`, … — in-memory | Заменить репозиториями `NurSapat.Data` без смены моделей |
| `OrderSpecificationLine` (CAD UI) | Текстовые строки в `OrderCalculationState` — только превью CAD | Не использовать для биллинга; источник истины — `CostSpecificationLine` |

### Legacy / можно удалить (после миграции)

| Элемент | Статус |
| ------- | ------ |
| `OrderItem` | **Удалён** — пустой stub, не использовался |
| `CustomerType` в `NurSapat.App.Models.Dictionaries` | **Удалён** — перенесён в Core |
| `Material` (Core PG) | **Legacy `[Obsolete]`** — канон `MaterialDetail` |
| `CustomerItem` / `SupplierItem` | **Временные алиасы** — заменить прямым использованием Core-моделей в App |
| `ConstructionModel` | **UI-обёртка** — не доменная сущность, не переносить в PG |
| `OrderSpecificationLine` (CAD) | **Legacy display** — не таблица спецификации |
| `CadToolbarRegistry` | **Dead code** |
| `WarehouseItem` | **Не существует** — роль выполняют `StockDocumentLine` + `FinishedGoodItem` |

### Drawing / Template / Hardware

| Сущность | Где живёт | Готовность |
| -------- | --------- | ---------- |
| **Drawing** | `ConstructionDrawingStore`, `DrawingExportService` — файлы/экспорт, не доменная модель | Отдельная таблица `drawings` при PG — по `OrderConstructionId` |
| **Template** | `ConstructionTemplateSerializer`, `CadTemplateStore` (CAD) | Шаблоны геометрии; не смешивать с ERP `OrderTemplateItem` |
| **Hardware** | `CalculationEngine.HardwareKit`, `HardwareAutoPickRule`, элементы `GeometryElementKind.Handle/Hardware` | Расчёт через `MaterialId` категории Hardware; детализация по SKU — в backlog |

### Сериализация JSON ↔ PostgreSQL

- **Schema v2** (`ProjectFormat.CurrentSchemaVersion = 2`): `SpecificationLines`, расширенный `GeometryElementData`.
- Старые `.nsp` v1: при открытии `SpecificationLines` пусты — пересчёт через `OrderCalculationService`.
- Поля DTO (`*Data` в `ProjectDocumentData`) — прямой прототип будущих колонок PG для заказа.
- ERP-справочники и склад пока **не** в `.nsp` — отдельные таблицы при миграции.

---

## Hardcode калькуляции / стоимости

**Закрыто на этапе 6.** Stub-формулы удалены.

| Файл | Статус |
| ---- | ------ |
| `OrderCalculationService.cs` | Модульный расчёт через `ConstructionCostCalculator` |
| `OrderCalculationState.cs` | Убрана формула `area × 5000`; стоимость 0 до синхронизации с листом заказа |
| `ExportService.BuildMaterialLines` | Строки из `SpecificationLines` |
| `CostTableViewModel` | Строки из `SpecificationLines` с unit price |




## Прочие временные элементы


| Файл                                                             | Описание                                                      |
| ---------------------------------------------------------------- | ------------------------------------------------------------- |
| `NurSapat.Cad.UI/Diagnostics/HorizontalImpostDragDiagnostics.cs` | Диагностика drag с `IsEnabled = true` по умолчанию            |
| `NurSapat.Cad.UI/Services/CadToolbarRegistry.cs`                 | Legacy Delphi-style реестр (~100 кнопок), **не используется** |
| `NurSapat.App/ViewModels/AdditionalDashboardViewModel.cs`        | Частично hardcoded user/stages/templates                      |


**Рекомендация для всех stores:** единый persistence слой через `NurSapat.Data` (PostgreSQL/SQLite) или файловая БД с транзакционной записью; demo-seed только при первом запуске.

---



# Неиспользуемый код


| Файл                                     | Тип                   | Описание                                             | Рекомендация                                          |
| ---------------------------------------- | --------------------- | ---------------------------------------------------- | ----------------------------------------------------- |
| `CadToolbarRegistry.cs`                  | Dead code             | Нет ссылок кроме самого файла                        | Мигрировать полезное в `CadToolbarLayout` или удалить |
| `CadTemplateSerializer.cs`               | Dead code (commented) | Закомментирован целиком                              | Восстановить или удалить                              |
| `OrderEditView` + `OrderEditViewModel`   | Вспомогательный UI     | Хостит `OrderSheetView`; не основной поток workspace      | Оставить или интегрировать в навигацию                |
| `WorkspaceSectionViewModel`            | Legacy                 | Заменён `ErpReferenceHubViewModel`; DataTemplate остаётся | Удалить при cleanup App.axaml                         |
| `EmptyCommand` (при misuse)              | Мёртвая семантика     | No-op в истории                                      | Ограничить API                                        |
| Конвертеры ConvertBack → throw           | Partial API           | `BoolToClassConverter`, `CollapseIconConverter`, др. | Гарантировать OneWay bindings                         |


**Не считалось заглушками (домен CAD):** `FakeFrame` / `DummyFrame` / фиктивные импосты — реальные режимы конструкции, не mock-данные.

---



# Возможности рефакторинга


| Цель                                                                                     | Почему                                                     | Рекомендация                                                                               |
| ---------------------------------------------------------------------------------------- | ---------------------------------------------------------- | ------------------------------------------------------------------------------------------ |
| `CadEditorViewModel.cs` (~7850 + partials)                                               | Нарушение SRP: placement, impost, fillings, undo, UI state | Разбить: PlacementService, ImpostEditor, SelectionController, FillingsSync, ViewportBridge |
| `CadCanvasControl.cs` (~1135)                                                            | Input + paint + coupling                                   | Вынести interaction controller и renderer                                                  |
| `OrderSheetViewModel.cs` (~1300)                                                         | Sheet + preview + ERP fields                               | Extract preview / party / print                                                            |
| `EnvironmentSettingsViewModel`                                                           | Очень большой settings god-VM                              | Child VM по вкладкам                                                                       |
| `ProfileSegment.cs`                                                                      | Геометрия + cascade PropertyChanged                        | DTO геометрии + UI-wrapper с batch notify                                                  |
| Много `*Store.cs` с одинаковым List CRUD                                                 | DRY                                                        | `InMemoryStore<T>` → затем `IRepository<T>` в Data                                         |
| Dual toolbar (`CadToolbarRegistry` vs `CadToolbarLayout`, `OrderDrawingViewModel` tools) | Дублирование абстракций                                    | Один источник истины                                                                       |
| `MainWorkspaceViewModel` service locator                                                 | `App.ServiceProvider.GetRequiredService`                   | Constructor DI / factories                                                                 |


---



# Архитектурные замечания



## Что работает хорошо

- Разделение App / Core / Data / Cad.*
- CommunityToolkit MVVM (`[RelayCommand]`, `[ObservableProperty]`) широко используется
- Window services для справочников уменьшают logic в XAML
- Параметризованный Dapper в репозиториях материалов/пользователей
- BCrypt для master DB password (`DatabasePasswordService`)



## Проблемы



### Файл

`NurSapat.App/Program.cs` (регистрация ≈188–192)

### Тип проблемы

Архитектура / DI

### Описание

`CadEditorViewModel` как **Singleton** на всё приложение.

### Почему это проблема

Состояние чертежа/истории/материалов разделяется между заказами; риск «протекания» сессии и роста RAM.

### Что рекомендуется сделать

Scoped-per-document / factory на заказ + явный `Reset()` при New/Open.

---



### Файл

Множество App `*Store` vs `NurSapat.Data`

### Тип проблемы

Нарушение разделения ответственности

### Описание

UI-справочники — RAM + demo; Data layer покрывает в основном users/materials/logs/migration.

### Почему это проблема

Два параллельных мира данных; UI «впереди» persistence.

### Что рекомендуется сделать

Перенести словари/склад/заказы в Data repositories; App оставить как UI + orchestration.

---



### Файл

`MainWorkspaceViewModel.cs` ≈217–265

### Тип проблемы

Нарушение DI / тестируемость

### Описание

Service locator через `App.ServiceProvider`.

### Почему это проблема

Скрытые зависимости, сложнее unit-тесты.

### Что рекомендуется сделать

Инъектировать нужные сервисы/фабрики окон.

---



### SOLID / KISS / DRY (кратко)

- **SRP:** god-classes (`CadEditorViewModel`, canvas, settings, order sheet).
- **DRY:** похожие demo-stores и tool stubs.
- **KISS:** тулбар показывает десятки disabled-инструментов вместо минимального shipping-набора.
- **MVVM:** в целом соблюдается; code-behind окон в основном chrome (ок), canvas control — тяжёлый view-logic.

---



# Замечания по UI



## Готовые / относительно зрелые

- Login → MainWorkspace
- Меню File/Order/Edit/View/Dictionaries/Settings/Service/Utilities/Help (команды в основном привязаны)
- CAD editor: верхняя панель параметров, тулбар режимов Создание/Редакт./Декор, холст, статусы
- Лист заказа (`OrderSheetView`)
- Окна справочников/склада/настроек/help/диалоги ERP



## Частично готовые / временные


| Поверхность                                          | Проблема                                                     |
| ---------------------------------------------------- | ------------------------------------------------------------ |
| CAD toolbar Creation/Editing                         | **Все инструменты активны** (этап 4)                                                                                 |
| Dashboard (legacy)                                   | **Встроенные справочники**                                   |
| OrderEditView                                        | **OrderSheetView**                                           |
| Workspace sections customers/materials/hardware/help | **ErpReferenceHubViewModel**                                 |
| Graphic symbols dictionary                           | **CRUD**                                                     |
| Orders dictionary actions                            | **Реальные операции + registry**                             |
| Cost table UI                                        | **Breakdown + material lines**                               |
| Login «Регистрация»                                  | **Работает**                                                 |
| Print dialog                                         | **Печатает (Windows)**                                       |




## Инструменты CAD — карта зрелости

- **Работают (по коду/layout):** все инструменты тулбара СОЗДАНИЕ / РЕДАКТИРОВАНИЕ / ДЕКОРИРОВАНИЕ, включая `split-profile`, zoom/pan/zoom-fit, выравнивания, арки, copy/paste, типовые, свойства, детали.
- **Ограничения модели:** произвольное деление polyline-профиля (см. этап 4).

---



# Замечания по производительности


| Файл / зона                    | Тип                    | Описание                                                                     | Рекомендация                             |
| ------------------------------ | ---------------------- | ---------------------------------------------------------------------------- | ---------------------------------------- |
| `CadCanvasControl`             | Лишние перерисовки     | Есть `ScheduleInvalidateVisual`, но часть путей зовёт raw `InvalidateVisual` | Всегда coalesced invalidate              |
| `CadEditorViewModel`           | PropertyChanged storms | Пачки `OnPropertyChanged` при zoom/selection/panels                          | Batch notify / scoped update flags       |
| `ProfileSegment`               | Notify cascade         | Сеттеры координат поднимают 8–10 свойств                                     | Suspend notifications при batch geometry |
| `CadEditorViewModel` hot paths | LINQ аллокации         | Частые `.ToList()` / `.Where().ToList()`                                     | Переиспользовать буферы / enumerate      |
| `OrderDrawingViewModel`        | Подписки               | PropertyChanged / SceneInvalidated без unsubscribe                           | `IDisposable`                            |
| `AdditionalDashboardViewModel` | Подписки               | Stage PropertyChanged без unload                                             | Unsubscribe / weak events                |
| `MainWorkspaceViewModel`       | Подписки               | `ProfileChanged +=` без `-=`                                                 | Dispose/close cleanup                    |
| Singleton CadEditor            | Утечка/рост RAM        | Документ + history на жизнь процесса                                         | Per-order scope + Reset                  |
| Drag diagnostics               | Overhead               | Diagnostics включены по умолчанию                                            | `#if DEBUG` / default off                |


---



# Замечания по безопасности


| Файл                            | Строка  | Тип                          | Описание                                    | Рекомендация                                            |
| ------------------------------- | ------- | ---------------------------- | ------------------------------------------- | ------------------------------------------------------- |
| `NurSapat.App/appsettings.json` | 3       | Секрет в репо                | `Password=7720` в connection string         | User secrets / env; ротация пароля; не коммитить        |
| `Program.cs`                    | ≈244    | Hardcoded fallback           | Тот же postgres password в коде             | Fail closed без пароля в source                         |
| `LoginViewModel.cs`             | 98, 178 | Plaintext credential         | `SavedPassword` в settings JSON             | DPAPI / Credential Manager / opaque token               |
| `EnvironmentSettings.cs`        | 73      | Plaintext                    | `MailPassword` сериализуется в профиль      | Шифрование at rest                                      |
| `SettingsService` / profile IO  | —       | Нет try/catch / atomic write | Падение / битый JSON                        | try/catch + temp+replace                                |
| `ConstructionElementsStore`     | —       | File IO                      | Без надёжной обработки ошибок               | Guard + log; не маскировать corrupt seed’ом без сигнала |
| `DataMigrationService`          | ≈110    | Динамический SQL             | `$"INSERT INTO {table}"`                    | Whitelist имён таблиц                                   |
| Репозитории материалов/users    | —       | OK                           | Параметризованный Dapper                    | Сохранять практику                                      |
| JSON import                     | —       | Риск размера                 | Без лимитов depth/size на недоверенный JSON | Лимиты при импорте проектов                             |


NullReference: в целом много `?.`, но god-class CAD-пути трудно аудировать без тестов — усилить nullable analysis и покрыть команды unit-тестами.

---



# Рекомендуемый порядок доработки



## Критично

1. **Убрать секреты из репозитория** (PostgreSQL password в `appsettings.json` и `Program.cs`); ротация.
2. **Не хранить пароль логина/почты в plaintext** на диске.
3. ~~**Заменить stub-калькуляцию стоимости**~~ — **этап 6 закрыт** (модульные калькуляторы + `MaterialsDetailPriceCatalog`).
4. ~~**Унификация моделей данных**~~ — **этап 7 закрыт**.
5. ~~**Финальная унификация доменной модели**~~ — **этап 8 закрыт** (роли материалов, `MaterialId`/`OrderId`, инварианты, артикулы CAD).



## Высокий

1. Persistence для orders/customers/stock/dictionaries через `NurSapat.Data` (перестать терять данные).
2. Разбить `CadEditorViewModel` + отказаться от глобального Singleton или жёсткий Reset на New/Open.
3. ~~Довести до конца оставшиеся CAD-инструменты~~ — **этап 4 закрыт** (все инструменты СОЗДАНИЕ/РЕДАКТИРОВАНИЕ/ДЕКОРИРОВАНИЕ активны; см. ограничения в разделе CAD).
4. ~~Довести печать и открытие заказа из справочника заказов~~ — **этап 5 ERP закрыт**.



## Средний

1. ~~Убрать Dashboard/OrderEdit/WorkspaceSection placeholders~~ — **этап 5 ERP закрыт**.
2. ~~Включить регистрацию; довести GraphicSymbols~~ — **этап 5 ERP закрыт**.
3. Нормализовать redraw/`PropertyChanged`; отписать event handlers; выключить drag diagnostics в release.
4. Персистентные CAD drawing templates (вместо hardcoded `CadTemplateStore`).
5. ~~Вывести `ConstructionVolumeLiters` в ERP-спецификацию~~ — **этап 4 закрыт**.
6. PostgreSQL persistence для справочников и склада (вместо demo-stores).



## Низкий

1. Удалить dead `CadToolbarRegistry` / commented `CadTemplateSerializer` / пустые ConvertBack риски (документировать OneWay).
2. DRY для stores; мелкий cleanup `EmptyCommand` / DialogService dummy RelayCommand.
3. Улучшить KISS тулбара (shipping subset vs full Delphi parity).

---



# Оценка готовности (приблизительно)


| Раздел                 | %         | Комментарий                                                                                                     |
| ---------------------- | --------- | --------------------------------------------------------------------------------------------------------------- |
| **UI**                 | **90%**   | CAD + ERP dashboard/справочники без placeholder-экранов                                               |
| **Логика**             | **92%**   | CAD полный; доменная модель консистентна, FK по Guid |
| **Архитектура**        | **82%**   | EF persistence-слой; переключение Demo/PG через DI |
| **Производительность** | **58%**   | Есть coalesce invalidate; hotspots PropertyChanged/LINQ/подписки |
| **Завершенность**      | **93%**   | Этапы 1–9: persistence реализован; orders store и справочники — частично Demo |
| **Общая готовность**   | **≈ 93%** | PG persistence готов; остаётся полная замена demo-stores и security hardening |


---

*Первоначальный аудит выполнен в режиме «анализ + исправление багов». Последовательно закрыты этапы 1–4 CAD, **этап 5 ERP**, **этап 6 Costing**, **этап 7**, **этап 8** (унификация домена) и **этап 9** (PostgreSQL Persistence).*