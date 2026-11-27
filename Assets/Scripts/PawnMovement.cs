using Fusion;
using UnityEngine;

public class PawnMovement : NetworkBehaviour
{
    // Lưu chỉ số ô hiện tại của quân cờ (0, 1, 2...). 
    // Tự động gọi OnStepChanged trên mọi máy khi giá trị thay đổi.
    [Networked, OnChangedRender(nameof(OnStepChanged))]
    public int CurrentStepIndex { get; set; } = 0;

    // Tham chiếu tự động đến DiceRoller trên Scene để lấy danh sách tọa độ các ô cờ
    private DiceRoller _diceRoller;

    public override void Spawned()
    {
        // Tự động tìm DiceRoller đang có sẵn trên Scene (không cần kéo thả thủ công vào Prefab)
        _diceRoller = FindAnyObjectByType<DiceRoller>();

        // Đổi màu quân cờ tự động theo ID người chơi để dễ phân biệt trên bàn cờ
        ApplyPlayerColor();

        // Đặt quân cờ vào vị trí ô xuất phát (Tile_0) ngay khi vừa sinh ra
        UpdatePawnPosition();
    }

    // RPC yêu cầu máy giữ quyền StateAuthority của quân cờ này cập nhật bước đi mới
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_MoveSteps(int steps)
    {
        if (_diceRoller == null)
        {
            _diceRoller = FindAnyObjectByType<DiceRoller>();
        }

        if (_diceRoller != null && _diceRoller.boardTiles != null && _diceRoller.boardTiles.Length > 0)
        {
            int totalTiles = _diceRoller.boardTiles.Length;
            int newStep = CurrentStepIndex + steps;

            // --- ĐIỀU KIỆN CHIẾN THẮNG ---
            // Nếu di chuyển đến ô cuối cùng (hoặc vượt qua ô cuối)
            if (newStep >= totalTiles - 1)
            {
                newStep = totalTiles - 1;
                int playerId = Object != null ? Object.StateAuthority.PlayerId : -1;
                Debug.Log($"[Bàn cờ] CHÚC MỪNG! Player {playerId} ĐÃ CHIẾN THẮNG TRÒ CHƠI!");
            }
            // --- Ô ĐẶC BIỆT / Ô BẪY ---
            // Ví dụ: Nếu đi vào ô số 3, dẫm phải bẫy nên bị lùi lại 1 bước
            else if (newStep == 3)
            {
                Debug.Log("[Bàn cờ] Dẫm phải ô bẫy tại Tile_3! Bị lùi lại 1 bước.");
                newStep = Mathf.Max(0, newStep - 1); // Đảm bảo không lùi quá ô 0
            }

            CurrentStepIndex = newStep; // Cập nhật chỉ số ô hiện tại
        }
    }

    // Hàm tự động chạy trên tất cả các máy khi CurrentStepIndex thay đổi
    private void OnStepChanged()
    {
        UpdatePawnPosition();
    }

    // Dịch chuyển quân cờ đến tọa độ của ô tương ứng trên bàn cờ
    public void UpdatePawnPosition()
    {
        if (_diceRoller == null)
        {
            _diceRoller = FindAnyObjectByType<DiceRoller>();
        }

        if (_diceRoller != null && _diceRoller.boardTiles != null && CurrentStepIndex >= 0 && CurrentStepIndex < _diceRoller.boardTiles.Length)
        {
            Transform targetTile = _diceRoller.boardTiles[CurrentStepIndex];
            if (targetTile != null)
            {
                // Tạo độ lệch nhỏ theo trục Z dựa vào PlayerId để khi 2 quân cờ đứng chung 1 ô không bị đè khít lên nhau
                int playerId = Object != null ? Object.StateAuthority.PlayerId : 0;
                float zOffset = (playerId % 2 == 0) ? -0.3f : 0.3f;
                transform.position = targetTile.position + new Vector3(0f, 0.6f, zOffset);
                Debug.Log($"[Bàn cờ] Quân cờ của Player {playerId} đã di chuyển đến ô số: {CurrentStepIndex}");
            }
        }
    }

    // Tự động tô màu khác nhau cho quân cờ của mỗi người chơi
    private void ApplyPlayerColor()
    {
        Renderer rend = GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            int playerId = Object != null ? Object.StateAuthority.PlayerId : 0;
            Color[] colors = { Color.red, Color.cyan, Color.green, Color.yellow, Color.magenta };
            rend.material.color = colors[Mathf.Abs(playerId) % colors.Length];
        }
    }
}
