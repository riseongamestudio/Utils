# Lớp nền

[← RiseOn.Utils](../../README.md)

`MonoBehaviourExt`, `ScriptableObjectExt` và `SerializedScriptableObjectExt` thay
cho `MonoBehaviour`, `ScriptableObject` và `SerializedScriptableObject` của Odin.
Chúng thêm cache transform và cặp hàm Undo / dirty cho code sửa dữ liệu trong
Editor.

## MonoBehaviourExt

| Thành viên | Ý nghĩa |
|---|---|
| `TF` | `transform`, được cache |
| `RectTF` | `RectTransform`, được cache; `null` nếu object không có |
| `RecordForUndo(target = null)` | Ghi trạng thái trước khi sửa. Bỏ trống thì ghi chính component |
| `RecordForUndo(params targets)` | Ghi nhiều object một lần |
| `MarkDirty(target = null)`, `MarkDirty(params targets)` | Đánh dấu đã sửa: `SetDirty`, ghi override của prefab instance, đánh dấu scene dirty |

Hai hàm Undo là `protected`. Chúng bỏ qua khi đang Play, và bị xóa khỏi bản build
cùng các tham số.

```csharp
public class Door : MonoBehaviourExt {
    [SerializeField] private float openAngle = 90;

    [Sirenix.OdinInspector.Button]
    private void OpenInEditor() {
        RecordForUndo(TF);
        TF.localRotation = Quaternion.Euler(0, openAngle, 0);
        MarkDirty(TF);
    }
}
```

## ScriptableObjectExt và SerializedScriptableObjectExt

Có cùng cặp `RecordForUndo` / `MarkDirty`. `SerializedScriptableObjectExt` kế thừa
`SerializedScriptableObject` của Odin; dùng khi cần Odin serialize những kiểu
Unity không serialize được (`Dictionary`, interface...).

Cần ghi Undo cho object ở chỗ không kế thừa các lớp này thì dùng
[`UndoHelper`](../Helpers/README.md#undohelper).
