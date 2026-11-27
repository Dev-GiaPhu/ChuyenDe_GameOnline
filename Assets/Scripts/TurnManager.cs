using System.Collections.Generic;
using System.Linq;
using Fusion;
using TMPro;
using UnityEngine;

public class TurnManager : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    // Biến mạng lưu chỉ số lượt đi hiện tại, tự động gọi OnTurnChanged khi giá trị thay đổi
    [Networked, OnChangedRender(nameof(OnTurnChanged))]
    public int TurnIndex { get; set; } = 0;

    [Header("Giao diện UI")]
    public TextMeshProUGUI turnInfoText;

    // Cờ kiểm tra đối tượng đã được khởi tạo mạng thành công hay chưa
    private bool _isSpawned = false;

    public override void Spawned()
    {
        _isSpawned = true;
        UpdateTurnUI();
    }

    // Tự động cập nhật giao diện khi có người chơi mới vào phòng
    public void PlayerJoined(PlayerRef player)
    {
        UpdateTurnUI();
    }

    // Tự động cập nhật giao diện khi có người chơi thoát khỏi phòng
    public void PlayerLeft(PlayerRef player)
    {
        UpdateTurnUI();
    }

    /*
    // Ở Chương 1: Dùng để test chuyển lượt bằng phím Space.
    // Sang Chương 2 & 3: Phím Space được chuyển sang cho DiceRoller để đổ xí ngầu.
    void Update()
    {
        // Không xử lý nếu chưa kết nối vào phòng mạng
        if (!_isSpawned || Object == null || !Object.IsValid) return;

        // Nhấn phím Space để kết thúc lượt của mình (Dùng để kiểm thử ở Chương 1)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (IsMyTurn())
            {
                RPC_NextTurn(Runner.LocalPlayer);
            }
            else
            {
                Debug.LogWarning("Chưa tới lượt của bạn! Vui lòng chờ đối thủ.");
            }
        }
    }
    */

    // Lấy danh sách người chơi đang hoạt động, sắp xếp cố định theo PlayerId tăng dần
    public List<PlayerRef> GetSortedPlayers()
    {
        if (Runner == null) return new List<PlayerRef>();
        return Runner.ActivePlayers.OrderBy(p => p.PlayerId).ToList();
    }

    // Tìm người chơi đang giữ lượt hiện tại dựa trên TurnIndex
    public PlayerRef GetCurrentTurnPlayer()
    {
        List<PlayerRef> players = GetSortedPlayers();
        if (players.Count == 0) return PlayerRef.None;
        int safeIndex = Mathf.Abs(TurnIndex) % players.Count;
        return players[safeIndex];
    }

    // Kiểm tra xem máy cục bộ hiện tại có phải là người đang giữ lượt không
    public bool IsMyTurn()
    {
        if (!_isSpawned || Runner == null) return false;
        return Runner.LocalPlayer == GetCurrentTurnPlayer();
    }

    // RPC gửi yêu cầu chuyển lượt từ người chơi lên máy giữ StateAuthority
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_NextTurn(PlayerRef caller)
    {
        // Chốt chặn: Chỉ người đang giữ lượt mới được phép yêu cầu qua lượt
        if (caller != GetCurrentTurnPlayer()) return;
        TurnIndex++;
    }

    // Hàm tăng chỉ số lượt trực tiếp trên máy giữ quyền StateAuthority (được gọi từ DiceRoller)
    public void AdvanceTurnOnAuthority()
    {
        if (Object != null && Object.HasStateAuthority)
        {
            TurnIndex++;
        }
    }

    private void OnTurnChanged()
    {
        UpdateTurnUI();
    }

    public void UpdateTurnUI()
    {
        if (!_isSpawned || turnInfoText == null || Runner == null) return;
        List<PlayerRef> players = GetSortedPlayers();
        PlayerRef currentTurn = GetCurrentTurnPlayer();
        string myId = "Player " + Runner.LocalPlayer.PlayerId;
        string turnId = currentTurn == PlayerRef.None ? "Chưa có" : "Player " + currentTurn.PlayerId;
        string status = IsMyTurn() ? "<color=green>(ĐẾN LƯỢT BẠN!)</color>" : "<color=red>(Đang chờ đối thủ...)</color>";
        turnInfoText.text = $"Phòng: {players.Count} người | Bạn là: {myId} | Lượt của: {turnId}\n{status}";
    }
}
