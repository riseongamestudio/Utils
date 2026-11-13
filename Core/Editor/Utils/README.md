# Utils trong Editor

[← RiseOn.Utils](../../README.md)

Namespace `RiseOn.Utils.Editor`.

- [PrefabUtils](#prefabutils)
- [InspectorPropertyUtils](#inspectorpropertyutils)
- [RectFieldUtils](#rectfieldutils)
- [Foldout đệ quy](#foldout-đệ-quy)
- [InlineEditorImitator](#inlineeditorimitator)
- [Chọn enum: ChoiceFieldUtils](#chọn-enum-choicefieldutils)
- [Trang settings](#trang-settings)
  - [SettingsPage](#settingspage)
  - [SettingsSection](#settingssection)
  - [SettingsTabs](#settingstabs)
  - [Các ô dùng chung](#các-ô-dùng-chung)
  - [Lưu và tìm asset](#lưu-và-tìm-asset)
- [Bo góc cửa sổ: WindowCornerUtils](#bo-góc-cửa-sổ-windowcornerutils)
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
`SettingsSection.SetExpandedRecursively(property, true)`: bản này ghi luôn trạng thái gập mở
mà mục `SettingsSection.Foldout(property, ...)` giữ trong phiên, nên mục chưa vẽ lần nào cũng
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
- Enum vẽ bằng [`ChoiceFieldUtils`](#chọn-enum-choicefieldutils).
- `CanDraw(type)` cho biết kiểu có vẽ được không: số, `bool`, `string`, enum, vector, `Color`,
  `AnimationCurve`, `Gradient`, `UnityEngine.Object`.
- `Split` để một khe giữa hai nửa vừa dấu override của prefab, nên các dòng chia đôi thẳng
  cột với nhau.

## Foldout đệ quy

Alt+click một foldout bất kỳ trong Inspector của Odin thì mở hay gập cả nhánh bên dưới, như
Inspector và Hierarchy của Unity. `RecursiveFoldoutDrawer` bọc ngoài mọi property: thấy trạng
thái mở/gập của property vừa đổi trong lúc giữ Alt thì đặt cùng trạng thái cho mọi property
con, qua `SettingsSection.SetExpandedRecursively`. Foldout của Odin, của list và mục
`SettingsSection.Foldout(property, ...)` đều được mà drawer không phải làm gì.

Drawer tự vẽ foldout cho một property con, không phải property của chính nó (như dòng provider
trên trang Ads), thì tự chuyển Alt+click xuống:

```csharp
var expanded = EditorGUI.Foldout(rect, child.State.Expanded, title, true);
if (expanded != child.State.Expanded && Event.current.alt) SettingsSection.SetExpandedRecursively(child, expanded);
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

## Chọn enum: ChoiceFieldUtils

Mọi ô enum trong Editor mở cùng một cửa sổ chọn, drop-down `ChoiceSearchWindow`, thay cho
menu hệ thống của Unity và ô chọn của Odin:

- `ChoiceEnumDrawer` cho mọi enum Odin vẽ, `[DrawerPriority(0, 0, 0.9995)]`: ngay trên drawer enum
  của Odin (0.999), dưới mọi drawer khác. Attribute tự vẽ enum theo cách riêng (`[EnumToggleButtons]`,
  `[ValueDropdown]`, `[EnumPaging]`) và drawer viết cho một enum cụ thể (priority mặc định 1)
  vẫn vẽ theo cách của chúng.
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

Bản `SerializedProperty` vẽ qua `ChoiceEnumPropertyDrawer`, nên có đủ `[Flags]`, `—` và dấu
override của prefab. Giá trị chọn đến sau khi trang đã vẽ xong, nên bản nhận callback
(`Enum<T>`, `Enum(Rect, Enum, Action<Enum>)`) ghi giá trị trong callback: đọc lại
`SerializedObject` rồi ghi, như ô kiểu của tham số ở trang Remote Config. Bản nhận giá trị và
bản `InspectorProperty` có `search: true/false` để bắt có hoặc bỏ ô tìm.

## Trang settings

Dành cho người viết trang Project Settings của một pack. Mọi trang của các pack RiseOn
(Ads, Consent, Firebase, Remote Config, Analytics, IAP) vẽ bằng các phần dưới đây, nên các
trang thẳng hàng và giống nhau tới từng pixel.

### SettingsPage

Bọc `guiHandler` của `SettingsProvider`: lề trái phải như trang Player của Unity (tiêu đề, nhãn
và viền các mục cùng một đường), cột nhãn rộng theo cách Inspector tính (45% bề rộng trừ 40,
ít nhất 120).

```csharp
new SettingsProvider("Project/RiseOn/MyPack", SettingsScope.Project) {
    guiHandler = _ => {
        using var page = SettingsPage.Begin();
        Draw();
    }
};
```

### SettingsSection

Một mục của trang: thanh tiêu đề, viền bo `Radius` (3, bằng góc field của Unity) quanh thanh
và nội dung, màu theo nhóm của Odin. Tooltip của thanh giải thích mục. Các mục cách nhau bằng
khoảng giữa hai field.

```csharp
// Mục luôn mở, một nút ở đầu phải thanh.
using (var section = new SettingsSection("Items", "What the game sells.", buttons: 1, padded: false)) {
    if (section.Button(0, SdfIconType.Plus, "Add an item")) Add();
    for (var i = 0; i < items.Count; i++) DrawItem(SettingsSection.Row(i), i);
}

// Mục gập mở, gập sẵn lần đầu trong phiên.
using (var section = SettingsSection.Foldout("MyPack.Testing", "Testing", open: false)) {
    if (section.Open) DrawTesting();
}

// Một giá trị nằm ngay trên thanh, ở cột giá trị của trang.
using (var section = SettingsSection.Foldout("MyPack.Prompt", "Prompt", barValue: true)) {
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
| `padded` | `false` khi nội dung có khung riêng (tab, danh sách) hoặc là các dòng `Row`, chạm viền |
| `barValue` | Tiêu đề chỉ chiếm cột nhãn, cột giá trị trên thanh là `section.BarValue` |
| `barOnly` | Mục chỉ có thanh, hình như mục đang gập: cho một mục đứng giữa các mục gập mở cùng loại |

Vẽ mọi thứ trên thanh (nút, `BarValue`) trước, rồi mới đọc `section.Open`: lần đọc đầu bắt đầu
phần nội dung, phần này mờ dần và cắt theo khi gập mở. Vẽ thanh sau đó thì giá trị trên thanh
biến mất trong lúc gập mở.

Mục vẽ cho một property của Odin thì dùng `SettingsSection.Foldout(property, ...)`: trạng thái
gập mở là của chính property, nên Alt+click ở mục ngoài gập mở luôn mục này. Gọi
`SettingsSection.Restore(property)` trong `Initialize` của drawer để mục mở như lúc rời trang.
Trong lớp dữ liệu, `[SectionGroup("Title")]` gom các member thành một mục, thay cho
`[FoldoutGroup]` của Odin. Mặc định là mục luôn mở; các tùy chọn:

| Tùy chọn | Ý nghĩa |
|---|---|
| `Foldable` | Thanh gập mở được, trạng thái là của property như trên: cho mục dài hay ít dùng |
| `Closed` | Mục `Foldable` gập sẵn lần đầu trong phiên |
| `Paddingless` | Như `padded: false` |
| `Tooltip` | Lời giải thích hiện khi rê chuột lên thanh |

```csharp
[SectionGroup("Formats", Paddingless = true, Foldable = true, Tooltip = "Which providers serve each format.")]
```

Dòng trong mục `padded: false`:

- `SettingsSection.Row(i)` một dòng cao 22 (`Height`), dòng lẻ tối hơn (`Shade`), dòng dưới
  chuột tô `Hover`, đúng màu hover của list Unity (#303030 tối, #B2B2B2 sáng);
  `RowContent` là chỗ đặt control giữa dòng; `RowButton(row, i)` là ô nút thứ `i` tính từ phải,
  thẳng cột với nút trên thanh. List tự xếp dòng thì gọi `RowBackground(row, i)` cho sọc và
  hover, ở mọi event, trước nội dung của dòng.
- `SettingsSection.List(reorderableList)` vẽ `ReorderableList` không đầu không đuôi, dòng chạm
  viền. Để dòng cao và sọc như `Row`: đặt
  `elementHeight = RowHeight(EditorGUIUtility.singleLineHeight) - ListRowPadding` và gọi
  `RowBackground(rect, index)` trong `drawElementBackgroundCallback` (như danh sách Products của
  trang IAP).
- `SettingsSection.IconButton(rect, icon, tooltip)` là nút một icon cỡ `IconSize` (12), màu
  của nhãn, sáng lên dưới chuột, icon nằm giữa `rect` tới từng pixel ở mọi tỉ lệ màn hình. Mọi
  nút icon của trang vẽ bằng nó; nút có chữ (như nút sửa trong hộp báo) là nút thường của Unity.
- `SettingsSection.TextField(rect, text, icon, tooltip, shown, out clicked)` là ô chữ có một nút
  icon nằm trong đầu phải ô, như nút xóa của ô tìm: cho việc làm trên chữ của ô, như trả về chữ
  mặc định (câu hỏi tracking của trang Consent). Nút hiện khi `shown`, chữ chừa chỗ cho nó; chuột
  trên nút là mũi tên; bấm nút thì ô thôi gõ và `clicked` là `true`.

Chiều cao xin từ layout luôn là số point nguyên (`RowHeight`), như control của Unity: layout
của Unity đặt mỗi phần tử ở point nguyên, nên chiều cao lẻ làm khoảng cách giữa các mục lúc 1,
lúc 2, lúc 3 point. Căn theo pixel chỉ làm lúc vẽ (`PixelUtils`).

Drawer tự đặt control trong mục thì dùng các số sau của `SettingsSection` để thẳng cột với các
dòng thường của trang:

| Thành phần | Dùng khi |
|---|---|
| `Inset` (4) | Khoảng từ viền mục tới nội dung, trái và phải: nhãn trong mục bắt đầu ở `row.x + Inset` |
| `LabelGap` (2) | Khoảng Unity để giữa cột nhãn và ô: ô tự đặt bắt đầu ở `row.x + Inset + labelWidth + LabelGap` |
| `Indent` (15) | Nội dung gập dưới foldout của một dòng thụt chừng đó, cột nhãn hẹp đi chừng đó |
| `ButtonRect(i)`, `RowButton(row, i)` | Ô của nút thứ `i` tính từ phải, trên thanh hay trên dòng, để đặt control khác cạnh nút |
| `BarValueInset` | Khoảng `BarValue` kết thúc trước mép mục: ô trên thanh chia cột tính từ mép trang thì trừ đi khoảng này |
| `RightInset`, `PushRightInset`, `PopRightInset` | Khoảng nội dung đang vẽ kết thúc trước mép phải trang. Drawer tự thêm lề phải cho nội dung của nó thì `Push` lề đó trước khi vẽ, `Pop` sau, để dòng chia cột bên trong vẫn thẳng cột với trang |
| `BeginFade(key, open)`, `EndFade()` | Fade gập mở của Odin cho nội dung gập dưới một dòng, tự vẽ lại cửa sổ trong lúc fade. Gọi `EndFade` dù `BeginFade` trả gì |

### SettingsTabs

Tab ngang trong một mục `padded: false`, như tab nền tảng ở trang Player: tab đang chọn cùng
màu nền với mục, tab khác tối hơn. Đổi tab thì trang trượt ngang, chiều cao chuyển dần.

```csharp
tabs ??= new SettingsTabs(new[] { "Editor", "Android", "iOS" }, 0);
tabs.Draw(page => DrawPlatform(page));
```

### Các ô dùng chung

| Hàm | Vẽ gì |
|---|---|
| `PlatformFieldUtils.Draw(label, android, ios)` | Một giá trị Android và một giá trị iOS dưới một nhãn, hai nửa cột giá trị; cửa sổ hẹp tới mức mỗi ô dưới `FieldMinWidth` (250) thì mỗi ô một dòng |
| `HelpBoxUtils.Draw(message, type, button)` | Hộp báo chữ cỡ nhãn của trang, icon cao một dòng, nút sửa (nếu có) nằm trong hộp ở đầu phải; trả `true` khi bấm nút |
| `ScriptUtils.Field(label, type)` | File script của một kiểu C#, như ô Script của component: chỉ đọc, bấm thì ping, bấm đúp thì mở. Trong lớp dữ liệu dùng `[ScriptField]` |
| `SpinnerUtils.LabelField(label, text)`, `SpinnerUtils.Content(text)` | Vòng quay chờ của Unity trước một dòng chữ, cho việc đang chạy (như trang Firebase lúc kiểm tra Firebase CLI). Cửa sổ tự vẽ lại khi vòng quay còn hiện; `Content` dùng được với mọi nhãn |

### Lưu và tìm asset

| Hàm | Việc |
|---|---|
| `DeferredSave.Request(asset)` | Gọi mỗi lần vẽ trang. Asset có thay đổi thì lưu xuống đĩa, nhưng chờ tới khi không còn ô chữ nào đang gõ: mỗi lần lưu ghi và import lại file mất khoảng 60 ms, gõ từng chữ mà lưu thì trang khựng. Sửa toggle hay chọn enum thì lưu ngay. Còn chờ thì lưu trước khi Unity compile lại và trước khi tắt |
| `AssetFindUtils.Paths(filter)` | Đường dẫn của `AssetDatabase.FindAssets(filter)`, giữ tới khi project đổi: mỗi lần tìm mất khoảng 25 ms, trang tìm asset mỗi lần vẽ thì chậm |

## Bo góc cửa sổ: WindowCornerUtils

Dành cho người viết cửa sổ Editor không viền (cửa sổ drop-down): bo các góc chọn của cửa sổ
theo `SettingsSection.Radius`, bằng góc của field và mục trong nó. Cửa sổ được cắt theo hình, viền và nội dung của root đi theo
đường cong. Chỉ bo góc được chọn, nên hai cửa sổ ghép liền (dòng điều khiển và danh sách của
`ChoiceSearchWindow`) bo như một.

```csharp
WindowCornerUtils.Show(window, window.ShowPopup);              // mở và giữ cửa sổ native vừa tạo
WindowCornerUtils.Round(window, WindowCornerUtils.Corners.All);
WindowCornerUtils.Round(window, WindowCornerUtils.Corners.All, hidden: true);   // cắt bỏ cả cửa sổ khi chưa có gì để hiện
```

Cắt giữ mọi pixel có tâm nằm trong đường cong, nên viền liền một nét, chỉ mép ngoài cùng hơi
bậc thang. Cửa sổ đổi cỡ thì tự cắt lại. Chạy trên Editor Windows (10 và 11 như nhau);
`IsSupported` là `false` trên Editor khác, ở đó cửa sổ giữ góc vuông.

## Vẽ theo pixel: PixelUtils, HoverUtils

- `PixelUtils.Snap(rect)` đặt cạnh của rect lên lưới pixel của màn hình, nên hai rect kề nhau
  không hở cũng không đè một pixel ở 125%, 150%; `PixelUtils.Pixel` là một pixel tính bằng point,
  bề dày của nét mảnh.
- `HoverUtils.Track(rect)` cho biết chuột có đang ở trên rect, và tự vẽ lại cửa sổ khi chuột ra
  vào: Inspector và Project Settings không vẽ lại khi chuột di chuyển, nên control sáng lên dưới
  chuột sẽ sáng mãi hoặc không sáng. Gọi ở mọi event, không chỉ lúc Repaint, để control giữ
  một id từ lúc layout tới lúc vẽ.
