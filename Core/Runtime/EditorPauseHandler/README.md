# EditorPauseHandler

[← RiseOn.Utils](../../README.md)

Dừng game trong Editor ở đúng lúc máy thật dừng Unity: khi hiện quảng cáo toàn màn
hình, hay popup ATT của iOS. Trên máy, hệ điều hành hay SDK dừng cả vòng lặp của Unity;
trong Editor không ai làm việc đó, nên các pack gọi `IEditorPauseHandler`.

## Pack gọi

```csharp
IEditorPauseHandler.Pause();    // thứ làm máy dừng game vừa hiện
IEditorPauseHandler.Resume();   // nó đã đóng
```

- Mỗi `Pause()` đi kèm một `Resume()`. Lời gọi lồng nhau được đếm: game dừng ở
  `Pause()` đầu tiên và chạy lại ở `Resume()` cuối cùng, như khi popup ATT bật lên
  lúc quảng cáo đang hiện.
- Ngoài Editor hai hàm không làm gì, vì máy tự dừng. Các pack RiseOn (xem danh sách
  dưới) chỉ gọi trong Editor, nên lời gọi tới được ngoài Editor là của game: hàm in một warning, một lần
  mỗi lần chạy.
- Chỉ gọi từ thread của Unity.

Pack đang gọi: `com.riseon.ads` (chuỗi quảng cáo toàn màn hình) và `com.riseon.consent`
(popup ATT giả trong Editor).

## Game tự định nghĩa cách dừng

Game không có lớp riêng thì `Pause()` đưa `Time.timeScale` về 0 và dừng âm thanh
(`AudioListener.pause`), `Resume()` trả lại như cũ. Game mà "dừng" còn là việc khác,
ví dụ đồng hồ tự đếm bằng thời gian thật, thì viết một lớp cài `IEditorPauseHandler`:

```csharp
public sealed class GameEditorPauseHandler : IEditorPauseHandler {
    void IEditorPauseHandler.OnPause()  => GameClock.Frozen = true;
    void IEditorPauseHandler.OnResume() => GameClock.Frozen = false;
}
```

- `OnPause` và `OnResume` là `protected`, nên lớp cài chúng tường minh và chỉ
  `IEditorPauseHandler` gọi được. Lớp không cần `partial`, và phải là `class`: generator
  chỉ tìm `class`, một `record` hay `struct` cài interface này bị bỏ qua.
- Source generator của pack tìm lớp này và sinh code đăng ký nó khi vào Play, chỉ trong
  Editor. Không cần gọi gì thêm.
- Lớp của game thay hẳn cách mặc định: muốn vẫn có `timeScale` về 0 thì tự làm trong
  `OnPause`.
- Lớp được compile cả vào bản build nhưng trên máy không ai tạo nó. Lớp dùng API của
  `UnityEditor` thì bọc cả lớp trong `#if UNITY_EDITOR`, không thì build player lỗi
  compile.

Analyzer báo lỗi compile khi:

| Mã | Khi nào |
|---|---|
| `RISEONUTILS001` | Lớp cài `IEditorPauseHandler` mà pack không tạo được: abstract, generic, lồng trong lớp khác, hay không có constructor không tham số (không `private`) |
| `RISEONUTILS002` | Có hơn một lớp cài `IEditorPauseHandler`, kể cả lớp kế thừa lớp của game |
| `RISEONUTILS003` | Một interface kế thừa `IEditorPauseHandler` |
