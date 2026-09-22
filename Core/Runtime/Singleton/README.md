# Singleton

[← RiseOn.Utils](../../README.md)

MonoBehaviour chỉ có một bản trong game, truy cập qua lớp hoặc qua interface.

## Dùng qua lớp

Kế thừa `Singleton<T>` với `T` là chính lớp đó:

```csharp
public class GameManager : Singleton<GameManager> {
    protected override void OnAwake() { /* khởi tạo, thay cho Awake */ }

    protected override void OnDestroy() {
        base.OnDestroy();   // bắt buộc: hủy đăng ký
    }
}

// nơi khác
GameManager.Ins.StartLevel();
```

| Thuộc tính | Ý nghĩa |
|---|---|
| `T.Ins` | Bản đang sống. Chưa có bản nào đăng ký thì tìm trong scene, kể cả object đang tắt; không thấy thì trả `null` |
| `T.HasIns` | Đã có bản đăng ký và còn sống. Không tìm trong scene |
| `T.ExistIns` | `Ins != null`, có tìm trong scene |

## Dùng qua interface

Cho interface kế thừa `ISingleton<I>`, rồi lấy chính interface làm `T`. Nơi dùng
chỉ cần biết interface:

```csharp
public interface IAudio : ISingleton<IAudio> {
    void Play(string id);
}

public class AudioManager : Singleton<IAudio>, IAudio {
    public void Play(string id) { /* ... */ }
}

IAudio.Ins.Play("click");
```

## Cấu hình trong Inspector

Foldout *Singleton*:

| Trường | Ý nghĩa |
|---|---|
| *Is Persistent* | Mặc định bật: gọi `DontDestroyOnLoad`. Chỉ có tác dụng với GameObject gốc, Inspector cảnh báo khi object có cha |
| *Destroy Duplicate Target* | Bản trùng xuất hiện sau sẽ bị hủy cả `GameObject` hay chỉ `Component` |

## Lưu ý

- `Awake` dùng nội bộ để đăng ký và không virtual; code khởi tạo đặt trong
  `OnAwake`.
- `T.Ins` tìm cả object đang tắt. Tìm thấy thì phần đăng ký và `OnAwake` của
  object đó chạy ngay, trước cả khi nó được bật.
- Một lớp có thể đăng ký dưới nhiều kiểu cùng lúc (lớp và các interface
  `ISingleton<>` nó cài). Kiểu đăng ký phải gán được từ lớp đó, sai thì báo lỗi.
