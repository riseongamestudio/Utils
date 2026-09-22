# Cửa sổ tìm kiếm

[← RiseOn.Utils](../../README.md)

Hai cửa sổ tìm kiếm dạng cây, gọi được từ drawer hay editor window tự viết.
Namespace `RiseOn.Utils.Editor.SearchWindow`.

```csharp
using RiseOn.Utils.Editor.SearchWindow;

// Chọn một kiểu component, gom theo đường dẫn AddComponentMenu.
if (GUI.Button(buttonRect, "Add")) {
    ComponentSearchWindow.Open(buttonRect, type => Debug.Log(type), searchText: "Collider");
}

// Chọn một object: tab Scene và tab Assets, lọc theo kiểu.
if (GUI.Button(buttonRect, "Pick")) {
    ObjectSearchWindow.Open(buttonRect, "Material",
        node => Debug.Log(node.Data as Material),
        filterType: typeof(Material),
        allowSceneObjects: false);
}
```

| Hàm | Tham số chính |
|---|---|
| `ComponentSearchWindow.Open(rect, onSelected, searchText)` | `onSelected` nhận `Type` được chọn |
| `ObjectSearchWindow.Open(rect, title, onSelected, filterType, rootPrefab, allowSceneObjects)` | `onSelected` nhận `SearchNode`, object nằm trong `node.Data`. `filterType` có thể là interface hay generic definition. `rootPrefab`: tìm trong prefab đang mở thay vì scene |

Tự làm cửa sổ tìm kiếm khác: kế thừa `SearchWindow`, khai các tab trong
`RegisterSections` bằng `AddSection(tên, builder)`. `builder` nhận
`SectionBuildContext`, dựng cây `SearchNode` (`Label`, `Icon`, `Data`,
`AddChild`), báo tiến độ bằng `ctx.ReportProgress` rồi gọi `ctx.Complete(root)`.
Mở cửa sổ bằng `Show(rect, title, onSelected, searchText)` từ một hàm `Open`
tĩnh như hai lớp có sẵn.
