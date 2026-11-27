# Utils trong Editor

[← RiseOn.Utils](../../README.md)

Namespace `RiseOn.Utils.Editor`.

- [PrefabUtils](#prefabutils)
- [InspectorPropertyUtils](#inspectorpropertyutils)
- [RectFieldUtils](#rectfieldutils)
- [Foldout đệ quy](#foldout-đệ-quy)
- [InlineEditorImitator](#inlineeditorimitator)
- [Chọn giá trị: ChoiceFieldUtils](#chọn-giá-trị-choicefieldutils)
- [Trang settings](#trang-settings)
  - [Khung một trang](#khung-một-trang)
  - [Attribute trong lớp dữ liệu](#attribute-trong-lớp-dữ-liệu)
  - [Section](#section)
  - [SectionRows](#sectionrows)
  - [SectionTabs](#sectiontabs)
  - [Các ô dùng chung](#các-ô-dùng-chung)
- [Vẽ theo pixel: PixelUtils, HoverUtils](#vẽ-theo-pixel-pixelutils-hoverutils)

## PrefabUtils

```csharp
var root = component.GetRootPrefab();             // null nếu không thuộc prefab
if (component.TryGetRootPrefab(out var prefabRoot)) { /* ... */ }
```

Trả GameObject gốc của prefab chứa object: prefab đang mở trong prefab stage,
hoặc prefab asset. Object nằm trong scene thì trả `null`.

## InspectorPropertyUtils

```csharp
property.SetExpandedRecursively(true);   // mở property và mọi property bên dưới
```

Chỉ đổi trạng thái của các property. Trên trang settings thì dùng
`Section.SetExpandedRecursively(property, true)`: bản này ghi luôn trạng thái gập mở
mà mục `Section.Foldout(property, ...)` giữ trong phiên, nên mục chưa vẽ lần nào cũng
mở đúng.

## RectFieldUtils

Vẽ giá trị của một `InspectorProperty` trên một dòng, trong rect cho sẵn chứ không theo
layout, cho drawer tự chia dòng mà các phần phải thẳng hàng tới từng pixel.

```csharp
RectFieldUtils.Split(valueRect, out var left, out var right);   // chia cột giá trị làm hai nửa
RectFieldUtils.Draw(easeProperty, left);
RectFieldUtils.Draw(curveProperty, right);
```

- `Draw` vẽ trong scope property của Unity, nên có dấu override của prefab; giá trị sửa xong
  ghi qua Odin, có Undo.
- Enum vẽ bằng [`ChoiceFieldUtils`](#chọn-giá-trị-choicefieldutils).
- `CanDraw(type)` cho biết kiểu có vẽ được không: số, `bool`, `string`, enum, vector, `Color`,
  `AnimationCurve`, `Gradient`, `UnityEngine.Object`.
- `Split` để một khe giữa hai nửa vừa dấu override của prefab, nên các dòng chia đôi thẳng
  cột với nhau.

## Foldout đệ quy

Alt+click một foldout bất kỳ trong Inspector của Odin thì mở hay gập cả nhánh bên dưới, như
Inspector và Hierarchy của Unity. `RecursiveFoldoutDrawer` bọc ngoài mọi property: thấy trạng
thái mở/gập của property vừa đổi trong lúc giữ Alt thì đặt cùng trạng thái cho mọi property
con, qua `Section.SetExpandedRecursively`. Foldout của Odin, của list và mục
`Section.Foldout(property, ...)` đều được mà drawer không phải làm gì.

Drawer tự vẽ foldout cho một property con, không phải property của chính nó (như dòng provider
trên trang Ads), thì tự chuyển Alt+click xuống:

```csharp
var expanded = EditorGUI.Foldout(rect, child.State.Expanded, title, true);
if (expanded != child.State.Expanded && Event.current.alt) Section.SetExpandedRecursively(child, expanded);
child.State.Expanded = expanded;
```

## InlineEditorImitator

Dành cho người viết drawer Odin: vẽ editor lồng giống `[InlineEditor]` cho một
`UnityEngine.Object` nằm bên trong kiểu wrapper, nơi Odin không tự vẽ được.

```csharp
private InlineEditorImitator inline;

protected override void Initialize() {
    inline = new InlineEditorImitator(
        isGameObjectType: typeof(TValue) == typeof(GameObject),
        extractUnderlyingObject: wrapper => ((Ref<TValue>)wrapper).value);
}

protected override void DrawPropertyLayout(GUIContent label) {
    var attr = Property.GetAttribute<InlineEditorAttribute>();
    inline.DrawLayout(Property, ValueEntry, attr, label, (rect, lbl) => DrawObjectField(rect, lbl));
}

public void Dispose() => inline.Dispose();   // hủy các editor đã tạo
```

Tôn trọng các tùy chọn của `InlineEditorAttribute` (`ObjectFieldMode`, preview,
...). `drawBaseFieldAction` vẽ ô chọn object ở vùng `rect` được đưa vào.

## Chọn giá trị: ChoiceFieldUtils

Mọi ô enum, và mọi ô `[ValueDropdown]` của một giá trị, trong Editor mở cùng một cửa sổ chọn,
drop-down `ChoiceSearchWindow`, thay cho menu hệ thống của Unity và ô chọn của Odin:

- `ChoiceEnumDrawer` cho mọi enum Odin vẽ, `[DrawerPriority(0, 0, 0.9995)]`: ngay trên drawer enum
  của Odin (0.999), dưới mọi drawer khác. Attribute tự vẽ enum theo cách riêng (`[EnumToggleButtons]`,
  `[EnumPaging]`) và drawer viết cho một enum cụ thể (priority mặc định 1) vẫn vẽ theo cách của chúng.
- `ValueDropdownChoiceDrawer` cho `[ValueDropdown]` của Odin trên một giá trị, mọi kiểu,
  `[DrawerPriority(0, 0, 2003)]`, ngay trên drawer của Odin: danh sách của attribute (giá trị
  thường hay `ValueDropdownItem` có tên) mở trong cửa sổ chọn. `[ValueDropdown]` trên list vẫn
  là của Odin, vì ở đó nó chọn nhiều phần tử thêm vào list.
- `ChoiceEnumPropertyDrawer` cho mọi enum Unity vẽ: Inspector không có Odin,
  `EditorGUILayout.PropertyField`, phần tử của mảng và `List`.

Từ `SearchFrom` (11) lựa chọn trở lên thì cửa sổ có ô tìm. Enum `[Flags]` mở danh sách bật tắt
từng cờ, cửa sổ vẫn mở. Nhiều object chọn cùng lúc mà giá trị khác nhau thì ô hiện `—`. Tên
hiện là `[InspectorName]` nếu có, không thì tên enum tách chữ.

Người viết drawer hay trang settings vẽ tay một ô enum thì gọi nó:

```csharp
ChoiceFieldUtils.Enum(new GUIContent("Region"), serialized.FindProperty("region"));    // SerializedProperty, theo layout
ChoiceFieldUtils.Enum(rect, property);                                                // InspectorProperty của Odin, trong rect
ChoiceFieldUtils.Enum(new GUIContent("Access"), FirebaseAccess.Mode, mode => FirebaseAccess.Mode = mode);
```

Danh sách giá trị có tên không phải enum (như sorting layer theo id) thì gọi
`ChoiceFieldUtils.Value(label, choices, current, onPicked)`, hay bản nhận `rect`; `choices` là
các cặp `(tên, giá trị)`, giá trị không có trong danh sách thì ô hiện chính nó.

Bản `SerializedProperty` vẽ qua `ChoiceEnumPropertyDrawer`, nên có đủ `[Flags]`, `—` và dấu
override của prefab. Giá trị chọn đến sau khi trang đã vẽ xong, nên bản nhận callback
(`Enum<T>`, `Enum(Rect, Enum, Action<Enum>)`) ghi giá trị trong callback: đọc lại
`SerializedObject` rồi ghi, như ô kiểu của tham số ở trang Remote Config. Bản nhận giá trị và
bản `InspectorProperty` có `search: true/false` để bắt có hoặc bỏ ô tìm.

## Trang settings

Dành cho người viết trang Project Settings hay inspector của một pack. Mọi trang của các pack
RiseOn (Ads, Analytics, Consent, Firebase, Remote Config, IAP) và inspector của Outline2D,
Wipe2D, Transitions vẽ bằng các phần dưới đây, nên chúng thẳng hàng và giống nhau tới từng
pixel. Viết trang mới thì làm theo khung ở [Khung một trang](#khung-một-trang).

### Khung một trang

Asset settings nằm trong Resources, trang vẽ asset qua cây Odin: các field mang attribute
(`[SectionGroup]`, `[SectionList]`, `[LabelText]`, `[Tooltip]`), trang chỉ vẽ hộp báo ở đầu
và phần không phải field.

```csharp
internal static class MySettingsProvider {
    internal const string PagePath = "Project/RiseOn/My Pack";

    private static readonly SettingsTree tree = new();

    [SettingsProvider]
    private static SettingsProvider Create() =>
        new(PagePath, SettingsScope.Project) {
            guiHandler = _ => {
                using var page = SettingsPage.Begin();
                if (SettingsAssetUtils.OutsideResources<MySettings>(MySettings.ResourceName) is { } path
                 && HelpBoxUtils.Draw($"{path} is outside a Resources folder.", MessageType.Error, new GUIContent("Move it"))) {
                    SettingsAssetUtils.MoveToResources<MySettings>(MySettings.ResourceName);
                }

                tree.Draw(SettingsAssetUtils.LoadOrCreate<MySettings>(MySettings.ResourceName));
            }
          , deactivateHandler = tree.Dispose
        };
}

[CustomEditor(typeof(MySettings))]
internal sealed class MySettingsInspector : SettingsAssetInspector {
    protected override string PagePath => MySettingsProvider.PagePath;
}
```

| Thành phần | Việc |
|---|---|
| `SettingsPage.Begin()` | Bọc `guiHandler`: lề trái phải như trang Player của Unity (tiêu đề, nhãn và viền các mục cùng một đường), cột nhãn rộng đúng công thức Inspector của Unity: 45% bề rộng trang làm tròn lên, trừ 40, ít nhất 120 |
| `SettingsTree` | Cây Odin của asset: tạo cho asset, tạo lại khi asset khác thế chỗ, mỗi sửa đổi có Undo và được ghi xuống đĩa qua `DeferredSave`. Giữ một cây cho mỗi trang, `Dispose` trong `deactivateHandler`. `Draw(asset)` vẽ mọi field; trang cần chen phần riêng giữa các field thì `var root = tree.Begin(asset)`, vẽ từng field bằng `root.Children["field"].Draw()`, rồi `tree.End()` |
| `SettingsAssetUtils` | Tìm, tạo và chuyển asset settings theo tên Resources của nó: `Find<T>(name)`, `FindPath<T>(name)`, `LoadOrCreate<T>(name)` (tạo ở `Assets/RiseOn/Resources`), `OutsideResources<T>(name)` (đường dẫn khi asset nằm ngoài mọi thư mục Resources, game không đọc được; `null` khi ổn), `MoveToResources<T>(name)` |
| `SettingsAssetInspector` | Inspector của asset settings: một nút mở trang. Asset chỉ sửa trên trang, như asset settings của Input System, nên mọi sửa đổi ở một chỗ |
| `DeferredSave.Request(asset)` | Lưu asset khi không còn ô chữ nào đang gõ và không drop-down nào của `SearchWindow` đang mở: mỗi lần lưu ghi và import lại file mất khoảng 60 ms, gõ từng chữ hay bật tắt từng dòng trong drop-down mà lưu thì trang khựng. Sửa đổi vẫn vào asset ngay, chỉ việc ghi xuống đĩa chờ. `SettingsTree` đã gọi; trang tự sửa asset ngoài cây thì gọi mỗi lần vẽ |
| `AssetFindUtils.Paths(filter)` | Đường dẫn của `AssetDatabase.FindAssets(filter)`, giữ tới khi project đổi: mỗi lần tìm mất khoảng 25 ms, trang tìm asset mỗi lần vẽ thì chậm |

Field trên trang lấy nhãn ngắn bằng `[LabelText]` khi tên field bị cột nhãn cắt, giữ
`[Tooltip]` làm lời giải thích. Giới hạn số dùng `[MinValue]` của Odin: `[Min]` của Unity
khiến Odin nhường field cho drawer của Unity, field đó mất nhãn ngắn và lệch cột.

### Attribute trong lớp dữ liệu

Các attribute ở `RiseOn.Utils` (runtime, chỉ có trong Editor) đặt mục của trang ngay trên field:

| Attribute | Vẽ gì |
|---|---|
| `[SectionGroup("Title")]` | Gom các member mang nó thành một mục, thay cho `[FoldoutGroup]` của Odin |
| `[SectionList]` | Một list hay mảng thành một mục: + trên thanh thêm, × ở dòng xóa, kéo tay nắm ở đầu dòng để đổi chỗ, kéo object từ Project hay Hierarchy vào để chèn (list object hay list có `Drop`). Thanh lấy nhãn và tooltip của member |
| `[NoRowShade]` | Đặt cùng `[SectionList]`, hay trên một `EnumMap` của `com.riseon.serializables`: các dòng không tô sọc xen kẽ. Dùng khi mỗi dòng là một mục riêng, như một mục gập mở, vì thanh của mục đã tách các dòng. Đặt trên list, hoặc trên member bọc list (như `ListSerRef`, `ListSerObject`) |
| `[SectionReference]` | Một member `[SerializeReference]` mà Editor tự chọn lớp (như lớp quản lý duy nhất của game kế thừa từ lớp gốc của pack) thành một mục gập mở: script của giá trị nằm trên thanh, chỉ đọc, các field của nó bên trong. Giá trị không có field nào để hiện thì mục chỉ có thanh, không có mũi tên gập mở. Thanh không có ô chọn kiểu |

Tùy chọn của `[SectionGroup]`:

| Tùy chọn | Ý nghĩa |
|---|---|
| `Foldable` | Thanh gập mở được: cho mục dài hay ít dùng. Trạng thái gập mở là của property, nên Alt+click ở mục ngoài gập mở luôn mục này |
| `Closed` | Mục `Foldable` gập sẵn lần đầu trong phiên |
| `Paddingless` | Nội dung chạm viền, cho nội dung có khung riêng như tab |
| `Tooltip` | Lời giải thích hiện khi rê chuột lên thanh |
| `Toggle` | Tên một member `bool` trong mục, vẽ thành ô tick trên thanh trước tiêu đề: cho mục là một tính năng bật tắt (như bóng của Outline2D). Bấm ô vuông thì bật tắt, tick thì mục mở ra; bấm mũi tên hay tiêu đề thì chỉ gập mở, như header của một override trong Volume của Unity. Ô không tick thì mục vẫn gập mở và sửa được như mọi mục, để điền thiết lập trước khi bật |

```csharp
[SectionGroup("Shadow", Toggle = nameof(shadowEnabled))]
[SerializeField] private bool shadowEnabled;
[SectionGroup("Shadow")]
[SerializeField] private Vector2 shadowOffset;
```

Tùy chọn của `[SectionList]`:

| Tùy chọn | Ý nghĩa |
|---|---|
| `Item` | Tên một phần tử trong tooltip của + và ×, như `"device"`; mặc định là tên kiểu phần tử |
| `New` | Tên method hay biểu thức Odin trả phần tử mà + thêm, tìm trên đối tượng chứa list: cho phần tử cần giá trị riêng, như sản phẩm IAP có số ngẫu nhiên khác mọi sản phẩm khác. Mặc định là chuỗi rỗng, instance mới của lớp có constructor không tham số, hay giá trị mặc định |
| `Drop` | Tên method hay biểu thức Odin biến object kéo vào list thành phần tử, tìm trên đối tượng chứa list, object là tham số `obj` (`UnityEngine.Object`); trả mặc định hay `null` là không nhận. Cho list của kiểu bọc object, như `ListSerObject` |
| `Pick` | Tên member hay biểu thức Odin trả `Type` của object mà + chọn, như interface phần tử bọc; đi cùng `Drop`, nó biến object được chọn thành phần tử |
| `Foldable`, `Closed` | Như của `[SectionGroup]` |
| `TitleField` | Tên một member `string` của đối tượng chứa list, vẽ thành ô nhập ở chỗ tiêu đề, sau mũi tên; mục thành gập mở được: mũi tên và phần trống của cột tiêu đề sau ô gập mở, bấm vào ô là sửa chữ. Cho list đứng thay đối tượng chứa nó, khi đối tượng là một phần tử của list ngoài và có tên trên trang (như một plan gồm các bước). Dev viết drawer cho đối tượng chỉ vẽ list (ví dụ `Property.Children["steps"].Draw(null)`); không thì member này và `BarField` hiện thêm thành dòng riêng |
| `BarField` | Tên một member của đối tượng chứa list, vẽ ở cột giá trị trên thanh, kèm nhãn của nó (cột nhãn vừa đủ chữ); tiêu đề chỉ chiếm cột nhãn của trang. Cho giá trị duy nhất đi kèm list, như weight của plan |

List của một enum là một tập hợp: + chỉ đưa các giá trị list chưa có, dòng giữ giá trị của nó và
hiện tên, các giá trị theo thứ tự của enum, không kéo đổi chỗ.

List của một kiểu `UnityEngine.Object`, hay list có `Drop`, nhận object kéo từ Project hay
Hierarchy:

- Thả ở tay nắm, ở ×, ở khe giữa hai dòng hay dưới dòng cuối thì object chèn vào đúng chỗ đó.
  Khi đang kéo, các dòng dạt ra mở một ô ghi tên object, kéo nhiều object cũng chỉ một ô.
- Thả lên thanh tiêu đề thì object thêm vào cuối list; khi đang kéo, cả mục sáng lên.
- Thả lên phần nội dung của một dòng thì chỗ đó là của field trong dòng: ô object dưới con trỏ
  nhận, như ô object thay giá trị của nó; không có ô nào nhận thì thả không làm gì.
- List của một kiểu component nhận cả game object: list lấy component đầu tiên đúng kiểu của nó.
- Asset chỉ nhận asset; object trong scene nhận cả object trong scene.

List của một kiểu `UnityEngine.Object`, hay list có `Pick` và `Drop`, mở cửa sổ chọn object (cửa sổ của ô
`SerObject`) khi bấm +, như list object của Odin; object được chọn thêm vào cuối, chọn None thì
thêm một dòng trống (list có `New` thì thêm phần tử `New` cho).

```csharp
[SerializeField, SectionList(Item = "product", Foldable = true, New = "NewEntry")]
[Tooltip("Ids as in the Play Console.")]
private List<Entry> products = new();
```

### Section

Một mục vẽ bằng code, cho phần trang không phải field (file config, tài khoản trên máy) hay
mục có nút riêng trên thanh: thanh tiêu đề, viền bo `Radius` (3, bằng góc field của Unity)
quanh thanh và nội dung, màu theo nhóm của Odin. Các mục cách nhau bằng khoảng giữa hai field.

```csharp
// Mục luôn mở, một nút ở đầu phải thanh.
using (var section = new Section("Items", "What the game sells.", buttons: 1, padded: false)) {
    if (section.Button(0, SdfIconType.Plus, "Add an item")) Add();
    rows.Draw(items.Count, DrawItem, remove: Remove);
}

// Mục gập mở, gập sẵn lần đầu trong phiên.
using (var section = Section.Foldout("MyPack.Testing", "Testing", open: false)) {
    if (section.Open) DrawTesting();
}

// Một giá trị nằm ngay trên thanh, ở cột giá trị của trang.
using (var section = Section.Foldout("MyPack.Prompt", "Prompt", barValue: true)) {
    prompt = EditorGUI.TextField(section.BarValue, prompt);
    if (section.Open) DrawTranslations();
}
```

| Tham số | Ý nghĩa |
|---|---|
| `key` | Khác nhau giữa mọi mục trong project: trạng thái gập mở được giữ trong phiên theo nó |
| `title`, `tooltip` | Chữ trên thanh, và lời giải thích hiện khi rê chuột |
| `open` | Mở hay gập lần đầu trong phiên |
| `buttons` | Số nút ở đầu phải thanh; `section.Button(i, ...)` vẽ nút thứ `i` tính từ phải |
| `padded` | `false` khi nội dung có khung riêng (tab) hoặc là dòng của `SectionRows`, chạm viền |
| `barValue` | Tiêu đề chỉ chiếm cột nhãn, cột giá trị trên thanh là `section.BarValue`, kết thúc sát nút đầu tiên như giá trị của một dòng kết thúc sát nút × |
| `barOnly` | Mục chỉ có thanh, hình như mục đang gập: cho một mục đứng giữa các mục gập mở cùng loại |
| `titleField` | Chỉ ở `Section.Foldout(property, ...)`: `InspectorProperty` của một member `string`, vẽ thành ô nhập ở chỗ tiêu đề, sau mũi tên, rộng theo chữ (tối thiểu 120 để tên thường dài ngắn vẫn thẳng hàng); mũi tên và phần trống của cột tiêu đề sau ô gập mở, bấm vào ô là sửa chữ. Tooltip của ô lấy `[Tooltip]` của member; `title` không vẽ |

Vẽ mọi thứ trên thanh (nút, `BarValue`) trước, rồi mới đọc `section.Open`: lần đọc đầu bắt đầu
phần nội dung, phần này mờ dần và cắt theo khi gập mở. Vẽ thanh sau đó thì giá trị trên thanh
biến mất trong lúc gập mở.

Mục vẽ cho một property của Odin thì dùng `Section.Foldout(property, ...)`: trạng thái gập mở
là của chính property, nên Alt+click ở mục ngoài gập mở luôn mục này. Gọi
`Section.Restore(property)` trong `Initialize` của drawer để mục mở như lúc rời trang. Tham số
`toggle` vẽ một `bool` thành ô tick trên thanh, như tùy chọn `Toggle` của `[SectionGroup]`.

Phần tử dùng chung của mục:

- `Section.IconButton(rect, icon, tooltip)` là nút một icon cỡ `IconSize` (12), màu của nhãn,
  sáng lên dưới chuột, icon nằm giữa `rect` tới từng pixel ở mọi tỉ lệ màn hình. Mọi nút icon của
  trang vẽ bằng nó; nút có chữ (như nút sửa trong hộp báo) là nút thường của Unity.
- `Section.Icon(rect, icon, lit)` vẽ icon như `IconButton` mà không có nút, cho icon chỉ để
  nhìn hay kéo, như tay nắm của dòng.
- `Section.CutLabel` là style nhãn một dòng, hẹp hơn chữ thì cắt bằng "…" như nhãn của
  Unity: cho chữ trên dòng tự đặt, vì `GUI.Label` không kèm style thì xuống dòng, tràn khỏi
  dòng. `CalcSize` của nó chỉ ra phần padding; đo chữ bằng `EditorStyles.label`.
- `Section.TextField(rect, text, icon, tooltip, shown, out clicked)` là ô chữ có một nút icon
  nằm trong đầu phải ô, như nút xóa của ô tìm: cho việc làm trên chữ của ô, như trả về chữ mặc
  định (câu hỏi tracking của trang Consent). Nút hiện khi `shown`, chữ chừa chỗ cho nó; chuột trên
  nút là mũi tên; bấm nút thì ô thôi gõ và `clicked` là `true`.

Drawer tự đặt control trong mục thì dùng các số sau của `Section` để thẳng cột với các dòng
thường của trang:

| Thành phần | Dùng khi |
|---|---|
| `Inset` (4) | Khoảng từ viền mục tới nội dung, trái và phải: nhãn trong mục bắt đầu ở `row.x + Inset` |
| `LabelGap` (2) | Khoảng Unity để giữa cột nhãn và ô: ô tự đặt bắt đầu ở `row.x + Inset + labelWidth + LabelGap` |
| `Indent` (15) | Nội dung gập dưới foldout của một dòng thụt chừng đó, cột nhãn hẹp đi chừng đó |
| `ButtonWidth` (20) | Cạnh của ô vuông chứa một nút trên thanh hay trên dòng, và tay nắm của dòng |
| `ButtonRect(i)`, `RowButton(row, i)` | Ô của nút thứ `i` tính từ phải, trên thanh hay trên dòng, để đặt control khác cạnh nút |
| `RowHandle(row)` | Ô tay nắm ở đầu trái dòng, đối xứng với ô `RowButton(row, 0)` qua giữa thanh |
| `RowContent(row, height)` | Chỗ đặt control cao `height` trong một dòng như thanh, trên lưới pixel |
| `Shade` | Lớp tối phủ lên dòng lẻ, lấy theo chênh lệch màu dòng chẵn và lẻ của list Odin |
| `Bar`, `DrawDropLight(box)` | Ô của thanh, và vẽ mục sáng lên cho object kéo vào thanh; `box` là `GUILayoutUtility.GetLastRect()` ngay sau khi mục đóng |
| `DropOutline`, `DropFill` | Màu viền và màu nền của chỗ nhận object kéo từ ngoài vào: ô list mở ra, hay mục sáng lên |
| `DrawDragFrame(rect, fill)` | Khung của phần nội dung mà thứ đang kéo sẽ vào, như ô list mở ra hay dòng đang kéo đổi chỗ, có nền khi `fill`. Trong một mục, khung vẽ sau viền của mục, chạy từ viền trái sang viền phải; chạm cuối nội dung thì xuống tới đáy hộp, nên viền khung nằm trên viền hộp và góc bo theo hộp |
| `BeginFade(key, open)`, `EndFade()` | Fade gập mở của Odin cho nội dung gập dưới một dòng, tự vẽ lại cửa sổ trong lúc fade. Gọi `EndFade` dù `BeginFade` trả gì |

Chiều cao xin từ layout luôn là số point nguyên, như control của Unity: layout của Unity đặt mỗi
phần tử ở point nguyên, nên chiều cao lẻ làm khoảng cách giữa các mục lúc 1, lúc 2, lúc 3 point.
Căn theo pixel chỉ làm lúc vẽ (`PixelUtils`).

### SectionRows

Các dòng của một danh sách trong mục `padded: false`, cho danh sách vẽ bằng code (danh sách
field thì dùng `[SectionList]`, nó vẽ bằng chính `SectionRows`). Dòng sọc xen kẽ như list của
Odin, không sáng lên dưới chuột; mục nằm trong một dòng giữ nền riêng, sọc của dòng chừa nó ra.
`Shaded = false` bỏ sọc, cho dòng mà mỗi dòng là một mục riêng.
`isSelected` tô dòng được chọn bằng màu dòng được chọn của Unity; `select` nhận cú nhấn mà control
trong dòng không nhận, kèm phím bổ trợ (Ctrl, Cmd, Shift), để người gọi tự giữ lựa chọn nhiều dòng.
Danh sách rỗng hiện một câu gợi ý. Dòng đổi chỗ được có tay nắm ở đầu trái, dòng xóa được có × ở đầu phải; hai ô vuông
bằng nhau, đối xứng qua giữa thanh, ngang dòng đầu của phần tử. Kéo tay nắm thì phần tử theo
chuột, các phần tử khác trượt sang để mở chỗ nó sẽ rơi vào. Danh sách có `takes` và `drop` mở
chỗ như vậy cho object kéo từ ngoài vào, trừ khi con trỏ nằm trên phần nội dung của một phần tử:
chỗ đó là của field trong phần tử.

```csharp
private readonly SectionRows rows = new();

using (var section = new Section("Parameters", buttons: 1, padded: false)) {
    SectionRows.AddButton(section, 0, "language", choices, Add);
    rows.Draw(items.Count, i => DrawItem(i), "None", move: Move, remove: Remove, removeTooltip: "Remove the language");
}
```

| Tham số của `Draw` | Ý nghĩa |
|---|---|
| `count`, `drawItem` | Số phần tử, và hàm vẽ phần tử thứ `i` bằng GUILayout; cột nhãn đã chỉnh để giá trị thẳng cột với các field của trang |
| `emptyHint` | Câu xám hiện khi danh sách rỗng, như điều + thêm |
| `move` | Đổi phần tử từ chỗ thứ nhất sang chỗ thứ hai (tính khi nó đã rời chỗ cũ); `null` cho danh sách không đổi thứ tự |
| `remove`, `removeTooltip` | Xóa phần tử ở chỗ đó, và tooltip của ×; `null` cho danh sách không xóa |
| `marksFirstRow` | Drawer của phần tử gọi `SectionRows.MarkFirstRow(row)` cho dòng đầu của nó: chỉ dòng đó chừa chỗ cho tay nắm và ×, các dòng dưới bắt đầu dưới giữa tay nắm |
| `takes`, `drop` | Danh sách có nhận một object kéo từ ngoài vào không, và hàm chèn các object nhận được, theo thứ tự kéo, từ chỗ chúng rơi vào; `null` cho danh sách không nhận |

Lượt kéo kết thúc ở view khác thì view này không nhận thêm sự kiện kéo nào.
`SectionRows.HasDragEnded(type)` cho biết sự kiện đang xử lý báo lượt kéo đã xong (chuột di chuyển,
bấm, nhả, rời cửa sổ, hay một lần vẽ khi không còn kéo object nào): chỗ nào tự sáng lên cho object
kéo vào thì dùng nó để tắt.

Đổi chỗ và xóa chạy sau khi danh sách vẽ xong, nên phần tử không đổi giữa chừng. Mỗi danh sách
vẽ cần một `SectionRows` riêng, vì nó giữ thao tác kéo qua các event.

`SectionRows.AddButton(section, slot, what, choices, add)` là nút + trên thanh: `choices` là
`null` thì bấm là thêm; có `choices` thì mở cửa sổ chọn (có ô tìm khi hơn 12 lựa chọn); hết lựa
chọn thì nút mờ, tooltip nói mọi `what` đã có trong danh sách.

### SectionTabs

Tab ngang trong một mục `padded: false`, như tab nền tảng ở trang Player: tab đang chọn cùng
màu nền với mục, tab khác tối hơn. Đổi tab thì trang trượt ngang, chiều cao chuyển dần.

```csharp
tabs ??= new SectionTabs(new[] { "Editor", "Android", "iOS" }, 0);
tabs.Draw(page => DrawPlatform(page));
```

### Các ô dùng chung

| Hàm | Vẽ gì |
|---|---|
| `PlatformMarks.Android`, `PlatformMarks.IOS`, `PlatformMarks.Width(mark)`, `PlatformMarks.Draw(rect, mark)` | Icon của hai nền tảng, đúng icon Unity dùng trong Build Profiles, thay cho chữ Android và iOS ở chỗ một giá trị Android và một giá trị iOS đứng cạnh nhau (như `PlatformFieldUtils`); chỗ chỉ nói một nền tảng thì giữ chữ. Tooltip là tên nền tảng. Icon của Unity chừa lề trong suốt khác nhau quanh hình, nên `Width` là bề rộng phần hình thật, đo một lần từ icon; đặt icon theo bề rộng đó thì icon nào cũng cách ô của nó như nhau. `Draw` vẽ phần hình bắt đầu từ cạnh trái rect, giữa theo chiều dọc, tooltip phủ cả rect; `Size` là cạnh của cả icon (16) |
| `PlatformFieldUtils.Draw(label, android, ios)` | Một giá trị Android và một giá trị iOS dưới một nhãn, trên một dòng: mỗi ô sau icon nền tảng của nó (`PlatformMarks`), cách icon 2 point, hai ô rộng bằng nhau. Khi mỗi ô một dòng, hai icon chiếm bề rộng của icon rộng hơn, nên hai ô bắt đầu thẳng nhau. Cửa sổ hẹp tới mức mỗi ô dưới `FieldMinWidth` (250) thì mỗi ô một dòng; khi chuyển giữa một dòng và hai dòng, ô Android giãn hoặc co, ô iOS trượt tới chỗ mới và các dòng bên dưới trượt theo, cùng thời gian với cú trượt tab của `SectionTabs`; cửa sổ hay trang vừa mở thì cặp ô vào thẳng dạng đúng, không trượt (khung hình đầu theo bề rộng lần trước của cùng nhãn, chưa có thì một dòng). Bản `Draw(rect, ...)` cho dòng tự đặt (như thanh của mục) luôn một dòng; hết chỗ cho icon thì hai icon chia nhau, không đè lên nhau. Bản có thêm `toggle, ref androidOn, ref iosOn` gắn một nút bật/tắt liền bên phải mỗi ô, riêng cho từng nền tảng (như nút **Test** của ô unit id quảng cáo); nút liền vào cạnh phải ô nên đọc là của ô đó, hai cụm vẫn cách nhau như khi không có nút, và `FieldMinWidth` tính phần ô chữ, không gồm nút |
| `HelpBoxUtils.Draw(message, type, button, enabled)` | Hộp báo chữ cỡ nhãn của trang, icon cao một dòng, nút sửa (nếu có) nằm trong hộp ở đầu phải, rộng như ở hàng dưới; cửa sổ hẹp tới mức chữ không còn chỗ thì nút xuống góc phải dưới chữ, chữ chiếm cả bề ngang hộp. `enabled` là `false` thì nút mờ. Trả `true` khi bấm nút |
| `HelpBoxUtils.Draw(message, type, (content, enabled), ...)` | Như trên với nhiều nút cạnh nhau ở đầu phải, nút đầu tiên bên trái; trả số thứ tự nút được bấm, `-1` khi không bấm. Nút có `enabled` là `false` thì mờ, tooltip của nó nên nói lý do. Mỗi nút rộng ít nhất `HelpBoxUtils.ButtonWidth` (110), nhãn dài hơn thì rộng theo nhãn |
| `SettingsIssue` | Một lỗi cấu hình của pack: nền tảng nó liên quan (`Target`, `NoTarget` là mọi nền tảng), có chặn build không (`Blocking`), lời báo, nút sửa (`FixLabel`, `Fix`) và ô cần sửa tay (`FieldAsset`, `FieldPath` là đường dẫn serialize như `devKey`). `Draw()` vẽ hộp báo, tên nền tảng ở đầu, nút sửa rồi nút **Edit**; `SettingsIssue.Draw(issues)` vẽ cả danh sách. Trang Ads, Analytics, Consent, IAP dùng nó. Build check của pack lọc theo `Concerns(target)` và `Blocking`. Icon của hộp theo loại: `Blocking` icon lỗi, không chặn icon cảnh báo, `SettingsIssue.Info(message, fixLabel, fix)` icon thông tin. `Info` là hộp thông tin, không phải lỗi (`IsInfo` đúng): một điều về cách thiết lập đang chạy, như trang Ads báo pack đang chạy `DefaultAdsManager`, có thể kèm một nút. Nó không chặn build; pack tạo `Info` thì build check của nó phải bỏ issue có `IsInfo` khỏi các cảnh báo in ra |
| `SettingsFocus.Request(asset, path)` | Đưa trang tới một ô, như nút **Edit**: mở mọi mục đang gập chứa ô đó, cuộn tới, rồi click như người dùng (gửi sự kiện chuột vào cửa sổ trang): vào ô chữ, ô số để gõ được ngay; vào nhãn của ô khác (toggle, dropdown) để chọn ô mà không đổi giá trị. Ô do `SettingsTree` vẽ tự được tìm; ô trang tự vẽ thì gọi `SettingsFocus.Drawn(asset, path, rect)` ngay sau khi vẽ, với rect của ô chữ. `PlatformFieldUtils.Draw` nhận thêm `asset, androidPath, iosPath` để báo hai ô của nó. Trang tự gập phần mà cây của nó không biết (như dòng của một list tự vẽ) thì đọc `SettingsFocus.Asset`, asset của yêu cầu đang chờ, để mở phần đó ra |
| `CodeRename.Members(type, renames)`, `CodeRename.Find(type, renames)` | Đổi tên thành viên của một kiểu sinh ra ở chỗ script của project gọi qua tên kiểu (`Type.ten_cu` thành `Type.ten_moi`, trùng từ khóa C# thì thêm `@`), trong script dưới `Assets` và package embedded, giữ encoding của file; trả các file đã sửa. `Find` chỉ tìm, không sửa. Không theo `using static`. Cho nút Apply của trang sinh enum, như Analytics và Remote Config, để code vẫn compile khi đổi tên |
| `ScriptUtils.Field(label, type)` | File script của một kiểu C#, như ô Script của component: chỉ đọc, bấm thì ping, bấm đúp thì mở. Trong lớp dữ liệu dùng `[ScriptField]` |
| `SpinnerUtils.LabelField(label, text)`, `SpinnerUtils.Content(text)` | Vòng quay chờ của Unity trước một dòng chữ, cho việc đang chạy (như trang Firebase lúc kiểm tra Firebase CLI). Cửa sổ tự vẽ lại khi vòng quay còn hiện; `Content` dùng được với mọi nhãn |
| `RepaintUtils.Request()` | Vẽ lại cửa sổ đang vẽ GUI ở tick editor kế tiếp, cho những gì đang tự chuyển động (hiệu ứng, vòng quay, hover đang đổi dáng). Gọi mỗi lần vẽ khi còn chuyển động. Cần vì trang Project Settings là cửa sổ UI Toolkit: phần IMGUI bên trong chỉ vẽ lại khi được đánh dấu cần vẽ, nên `HandleUtility.Repaint` hay `GUIHelper.RequestRepaint` của Odin để hiệu ứng đứng giữa chừng tới khi chuột di. Mọi hiệu ứng trong Utils (gập mở, trượt tab, cặp ô Android/iOS chuyển giữa một và hai dòng, vòng quay) đều gọi nó |
| `PackAdapters` | Các adapter của một pack (mỗi cái một `PackAdapter`: tên hiện, tên package, thư mục cạnh Core trong repo của pack) trên trang settings của pack, như cửa sổ Package Manager. `Row(adapter)` cho `PackageRow` của adapter: phiên bản trong project, **Install** chọn phiên bản từ registry của Core (Core là thư mục local thì lấy thư mục cạnh Core), **Update**, **Uninstall** (khi manifest ghi package đó); `Installed` nhận `uninstall` để hỏi trước khi gỡ, `Missing` cho adapter chưa cài. `Load` đọc phiên bản một lần mỗi phiên, `Reload` đọc lại; `DrawUpdateAll` vẽ **Update All** trên thanh của một mục. Trang Ads vẽ chúng thành dòng của mục Mediations, trang Analytics lên thanh của mục mỗi dịch vụ |
| `PackageRow`, `PackageRow.Draw(value, row)` | Một package trang liệt kê (adapter của pack, adapter của một ad network): phiên bản trong project, bản mới nhất, việc cài và gỡ. `Draw` vẽ cột giá trị của dòng: phiên bản, mũi tên tới bản mới hơn, rồi **Install**, **Update**, **Uninstall** tùy lúc; có danh sách phiên bản thì **Install** hỏi phiên bản, còn đã cài thì bấm vào phiên bản để chọn bản khác. Mọi nút rộng ít nhất bằng **Uninstall** để nút các dòng thẳng cột; nút tự vẽ trên cùng cột lấy độ rộng từ `PackageRow.ButtonWidth(label)` |
| `PackageJobs` | Thay đổi package của các trang, mỗi lần một cái như cửa sổ Package Manager: `Change(what, add, remove)` thêm và gỡ trong một yêu cầu, lỗi ra Console là "`what` failed". `Button(rect, label, key)` là nút khởi động một thay đổi: tắt khi `IsRunning`, nút đã bấm quay vòng chờ tại chỗ (kèm chữ nếu đủ chỗ), rê chuột hiện `what`; `key` đặt tên nút trên trang. Control khác (như ô chọn phiên bản) gọi `Press(key)` trước khi bắt đầu và hỏi `Spins(key)` khi vẽ. `Removes(package)` cho biết package đang được gỡ, để trang không lấy lại class của nó trước khi domain reload. `Begin`, `End` cho SDK tự đổi package theo cách riêng (như AppLovin) |

## Vẽ theo pixel: PixelUtils, HoverUtils

- `PixelUtils.Snap(rect)` đặt cạnh của rect lên lưới pixel của màn hình, nên hai rect kề nhau
  không hở cũng không đè một pixel ở 125%, 150%; `PixelUtils.Pixel` là một pixel tính bằng point,
  bề dày của nét mảnh.
- `HoverUtils.Track(rect)` cho biết chuột có đang ở trên rect, và tự vẽ lại cửa sổ khi chuột ra
  vào. Nó bật `wantsMouseMove` cho cửa sổ đang vẽ: cửa sổ không bật thì không nhận sự kiện di
  chuột (Project Settings mặc định tắt, đo trên Unity 6.3), nên control sáng lên dưới chuột sẽ
  sáng mãi hoặc không sáng. Gọi ở mọi event, không chỉ lúc Repaint, để control giữ
  một id từ lúc layout tới lúc vẽ.