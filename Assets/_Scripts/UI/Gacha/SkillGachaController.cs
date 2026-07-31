using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스킬 가챠 창 컨트롤러.
/// 오른쪽: 보유 마도서 목록 / 왼쪽: 스핀 화면 + 뽑기 버튼.
/// 판정은 SkillBookUseService가, 연출은 SkillGachaPresenter가 담당하고
/// 이 클래스는 둘을 이어주고 화면 상태만 관리한다.
/// </summary>
public class SkillGachaController : MonoBehaviour
{
    [Header("창")]
    [SerializeField] private GameObject _root;
    [SerializeField] private Button _closeButton;

    [Header("마도서 목록 (오른쪽)")]
    [Tooltip("미리 배치해둔 슬롯들. 보유 종류보다 많으면 남는 건 자동으로 숨겨진다")]
    [SerializeField] private SkillGachaBookSlot[] _bookSlots;

    [Tooltip("보유한 마도서가 하나도 없을 때 표시할 안내")]
    [SerializeField] private GameObject _emptyNotice;

    [Header("뽑기 (왼쪽)")]
    [SerializeField] private Button _drawButton;
    [SerializeField] private Text _selectedNameText;
    [SerializeField] private SkillGachaPresenter _presenter;

    private readonly List<OwnedSkillBook> _owned = new List<OwnedSkillBook>();
    private readonly List<Sprite> _iconPool = new List<Sprite>();

    private SkillBookItemData _selected;

    private void Awake()
    {
        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(Close);
        }

        if (_drawButton != null)
        {
            _drawButton.onClick.AddListener(OnClickDraw);
        }

        if (_root != null)
        {
            _root.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveListener(Close);
        }

        if (_drawButton != null)
        {
            _drawButton.onClick.RemoveListener(OnClickDraw);
        }
    }

    /// <summary>가챠 창 열기 (NPC 상호작용 시 호출).</summary>
    public void Open()
    {
        if (_root != null)
        {
            _root.SetActive(true);
        }

        _selected = null;
        Refresh();
    }

    /// <summary>가챠 창 닫기.</summary>
    public void Close()
    {
        if (_root != null)
        {
            _root.SetActive(false);
        }
    }

    /// <summary>보유 마도서 목록과 버튼 상태를 다시 그린다.</summary>
    public void Refresh()
    {
        SkillBookQuery.GetOwnedBooks(_owned);

        // 선택했던 게 다 떨어졌으면 선택 해제
        if (_selected != null && SkillBookQuery.GetCount(_selected) <= 0)
        {
            _selected = null;
        }

        // 아직 선택이 없으면 첫 번째를 자동 선택
        if (_selected == null && _owned.Count > 0)
        {
            _selected = _owned[0].Book;
        }

        RefreshSlots();
        RefreshDrawButton();

        if (_emptyNotice != null)
        {
            _emptyNotice.SetActive(_owned.Count == 0);
        }
    }

    /// <summary>목록 슬롯 갱신.</summary>
    private void RefreshSlots()
    {
        if (_bookSlots == null)
        {
            return;
        }

        for (int i = 0; i < _bookSlots.Length; i++)
        {
            SkillGachaBookSlot slot = _bookSlots[i];

            if (slot == null)
            {
                continue;
            }

            if (i < _owned.Count)
            {
                slot.Bind(_owned[i].Book, _owned[i].Count, OnSelectBook);
                slot.SetSelected(_owned[i].Book == _selected);
            }
            else
            {
                slot.Clear();
            }
        }
    }

    /// <summary>뽑기 버튼 활성 여부와 선택 이름 갱신.</summary>
    private void RefreshDrawButton()
    {
        bool canDraw = _selected != null
            && SkillBookQuery.GetCount(_selected) > 0
            && (_presenter == null || _presenter.IsPlaying == false);

        if (_drawButton != null)
        {
            _drawButton.interactable = canDraw;
        }

        if (_selectedNameText != null)
        {
            _selectedNameText.text = _selected != null
                ? $"{_selected.itemName}  x{SkillBookQuery.GetCount(_selected)}"
                : "마도서를 선택하세요";
        }
    }

    /// <summary>목록에서 마도서를 골랐을 때.</summary>
    private void OnSelectBook(SkillBookItemData book)
    {
        if (_presenter != null && _presenter.IsPlaying)
        {
            return;
        }

        _selected = book;
        RefreshSlots();
        RefreshDrawButton();
    }

    /// <summary>뽑기 버튼.</summary>
    private void OnClickDraw()
    {
        if (_selected == null)
        {
            return;
        }

        if (_presenter != null && _presenter.IsPlaying)
        {
            return;
        }

        // 1) 결과를 먼저 확정한다 (연출은 이 결과를 보여주기만 한다)
        SkillBookResult result = SkillBookUseService.Use(_selected);

        if (result.Success == false)
        {
            Debug.Log("[SkillGacha] 사용 실패 (보유 수량 부족)");
            Refresh();
            return;
        }

        // 2) 스핀에 쓸 아이콘 모으기
        BuildIconPool(_selected);

        // 3) 연출 재생 후 목록 갱신
        if (_presenter != null)
        {
            RefreshDrawButton();
            _presenter.Play(result, _iconPool, Refresh);
        }
        else
        {
            Refresh();
        }
    }

    /// <summary>스핀 중 스쳐갈 아이콘 목록(해당 마도서 후보 스킬).</summary>
    private void BuildIconPool(SkillBookItemData book)
    {
        _iconPool.Clear();

        SkillData[] pool = book.CandidateSkills;

        if (pool == null)
        {
            return;
        }

        for (int i = 0; i < pool.Length; i++)
        {
            SkillData skill = pool[i];

            if (skill == null || skill.SkillIcon == null)
            {
                continue;
            }

            if (book.IsInTierRange(skill) == false)
            {
                continue;
            }

            _iconPool.Add(skill.SkillIcon);
        }
    }
}
