using Fusion;
using TMPro;
using UnityEngine;

public class DiceRoller : NetworkBehaviour
{
    // Lưu kết quả mặt xí ngầu vừa đổ (từ 1 đến 6)
    [Networked]
    public int DiceResult { get; set; } = 1;

    // Lưu tổng số lần đã đổ xí ngầu, kích hoạt OnDiceRolled mỗi khi có lượt đổ mới
    [Networked, OnChangedRender(nameof(OnDiceRolled))]
    public int RollCount { get; set; } = 0;

    // Lưu định danh của người chơi vừa thực hiện cú đổ xí ngầu
    [Networked]
    public PlayerRef LastRoller { get; set; }

    [Header("Tham chiếu Hệ thống & UI")]
    public TurnManager turnManager;
    public TextMeshProUGUI diceText;

    [Header("Danh sách các ô cờ trên Scene (Từ Tile_0 đến Tile_9)")]
    public Transform[] boardTiles;

    private bool _isSpawned = false;

    public override void Spawned()
    {
        _isSpawned = true;
        // Tự động tìm TurnManager gắn cùng trên đối tượng BoardManager nếu quên kéo trong Inspector
        if (turnManager == null)
        {
            turnManager = GetComponent<TurnManager>();
        }
    }

    void Update()
    {
        // Đảm bảo đối tượng đã khởi tạo mạng xong mới nhận phím bấm
        if (!_isSpawned || Object == null || !Object.IsValid) return;

        // Nhấn phím Space để đổ xí ngầu khi đến lượt
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Kiểm tra thông qua TurnManager xem hiện tại có đúng là lượt của máy mình không
            if (turnManager != null && turnManager.IsMyTurn())
            {
                int randomRoll = Random.Range(1, 7); // Sinh số ngẫu nhiên từ 1 đến 6
                RPC_RollDice(randomRoll, Runner.LocalPlayer);
            }
            else
            {
                Debug.LogWarning("Chưa tới lượt bạn đổ xí ngầu!");
            }
        }
    }

    // Gửi kết quả xí ngầu từ máy người chơi lên máy giữ quyền StateAuthority
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RollDice(int rollValue, PlayerRef roller)
    {
        // Chốt chặn bảo mật: Chỉ chấp nhận kết quả nếu người gửi đúng là người đang giữ lượt
        if (turnManager == null || roller != turnManager.GetCurrentTurnPlayer()) return;

        DiceResult = rollValue;
        LastRoller = roller;
        RollCount++; // Tăng số lần đổ để kích hoạt OnDiceRolled trên toàn bộ các máy

        // Tìm quân cờ thuộc sở hữu của người vừa đổ xí ngầu và ra lệnh di chuyển
        PawnMovement[] allPawns = FindObjectsByType<PawnMovement>(FindObjectsSortMode.None);
        foreach (PawnMovement pawn in allPawns)
        {
            if (pawn.Object != null && (pawn.Object.StateAuthority == roller || pawn.Object.InputAuthority == roller))
            {
                pawn.RPC_MoveSteps(rollValue);
                break;
            }
        }

        // Tự động chuyển lượt sang cho người chơi tiếp theo
        turnManager.AdvanceTurnOnAuthority();
    }

    // Hàm tự động chạy trên tất cả các máy khách khi RollCount tăng lên
    private void OnDiceRolled()
    {
        if (diceText != null)
        {
            diceText.text = $"Lượt đổ #{RollCount}: Player {LastRoller.PlayerId} đổ được <color=yellow>{DiceResult}</color> nút!";
        }

        // Cập nhật lại dòng thông báo lượt đi phía trên màn hình
        if (turnManager != null)
        {
            turnManager.UpdateTurnUI();
        }
    }
}
