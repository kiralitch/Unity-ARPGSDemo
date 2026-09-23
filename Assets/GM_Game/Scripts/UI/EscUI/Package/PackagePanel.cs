using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/*
 * 背包界面预制体函数
 * 
 */

public class PackagePanel : BasePanel
{
    /* UI 对象 */
    [SerializeField]
    private Transform UIMenu;
    [SerializeField]
    private Transform UIMenuPackage;
    [SerializeField]
    private Transform UIMenuFood;
    [SerializeField]
    private Transform UICloseBtn;
    [SerializeField]
    private Transform UIRightCenter;
    [SerializeField]
    private Transform UIScrollView;
    [SerializeField]
    private Transform UIDetailPanel;
    [SerializeField]
    private Transform UIDelectPanel;
    [SerializeField]
    private Transform UIDelectBackBtn;
    [SerializeField]
    private Transform UIDelectInfoText;
    [FormerlySerializedAs("UIDelectConfitmBtn")] [SerializeField]
    private Transform UIDelectConfirmBtn;
    [SerializeField]
    private Transform UIBottomMenus;
    [SerializeField]
    private Transform UIDelectBtn;
    [SerializeField]
    private Transform UIDetailBtn;

    public GameObject PackageUIItemPrefab; //背包格子物体预制件

    #region 缓存当前选中的物品细节
    //点击物品时所记录的物品
    private string chooseItemUid;
    private string LastChooseItemUid = null;

    public string ChooseItemUID
    {
        get { return chooseItemUid; }
        set
        {
            LastChooseItemUid = chooseItemUid;
            chooseItemUid = value;
            RefreshDetail();
        }
    }

    private void RefreshDetail()
    {
        PackageLocalItem localItem = GameManager.GetInstance.GetPackageLocalItemByUID(chooseItemUid);
        //刷新
        UIDetailPanel.GetComponent<PackageDetailPanel>().RefreshDetailPanel(localItem, this);
        //取消之前选中图片
        FreshLastItemSelectImage();
    }

    //将过去选中的特效删除
    private void FreshLastItemSelectImage()
    {
        if (LastChooseItemUid == null) return;
        PackageLocalItem LastChooseItem = GameManager.GetInstance.GetPackageLocalItemByUID(LastChooseItemUid);

        var ScrollContext = UIScrollView.GetComponent<ScrollRect>().content; //获取滚动条context父节点
        for (int i = 0; i < ScrollContext.childCount; i++)
        {
            var ItemCell = ScrollContext.GetChild(i);
            ItemCell.GetComponent<PackageCell>().SetLastSelectToZero(LastChooseItem);
        }
    }
    #endregion
    
    protected override void Awake()
    {
        base.Awake();
        InitUI();
        InitClick();
    }

    private void Start()
    {
        RefreshUI();
    }

    private void RefreshUI()
    {
        RefreshScroll();
    }

    private void InitUI()
    {
        UIDelectPanel.gameObject.SetActive(false);
        UIBottomMenus.gameObject.SetActive(true);
    }

    /* 初始化点击事件 */
    private void InitClick()
    {
        UIMenuPackage.GetComponent<Button>().onClick.AddListener(OnClickWeapon);
        UIMenuFood.GetComponent<Button>().onClick.AddListener(OnClickFood);
        UICloseBtn.GetComponent<Button>().onClick.AddListener(OnClickClose);

        UIDelectBackBtn.GetComponent<Button>().onClick.AddListener(OnDelectBack);
        UIDelectConfirmBtn.GetComponent<Button>().onClick.AddListener(OnDelectConfirm);
        UIDelectBtn.GetComponent<Button>().onClick.AddListener(OnDelect);
        UIDetailBtn.GetComponent<Button>().onClick.AddListener(OnDetail);
    }
    
    /* 刷新滚动条滚动窗口 */
    private void RefreshScroll()
    { 
        //清理现有滚动条内容
        var ScrollContext = UIScrollView.GetComponent<ScrollRect>().content; //获取滚动条context父节点
        for (int i = 0; i < ScrollContext.childCount; i++)
        {
            Destroy(ScrollContext.GetChild(i).gameObject);
        }

        //根据排序后的背包数据重新创建并初始化每一个物品格子
        foreach (var packageLocalData in GameManager.GetInstance.GetSortPackageLocalData())
        {
            //实例化物品格子的预制体，并挂到Content下
            Transform packageUIItem = Instantiate(PackageUIItemPrefab.transform, ScrollContext);
            PackageCell ItemCell = packageUIItem.GetComponent<PackageCell>(); //获取这个预制体的Cell组件
            ItemCell.RefreshCell(packageLocalData, this);
        }
    }
    
    #region 按钮监听全函数

    private void OnClickWeapon()
    {
        print(">OnClickWeapon");
    }
    
    private void OnClickFood()
    {
        print(">OnClickFood");
    }
    
    private void OnClickClose()
    {
        print(">OnClickClose");
        ClosePanel();
    }
    
    private void OnDelectBack()
    {
        print(">OnDelectBack");
    }
    
    private void OnDelectConfirm()
    {
        print(">OnDelectConfirm");
    }
    
    private void OnDelect()
    {
        print(">OnDelect");
    }
    
    private void OnDetail()
    {
        print(">OnDetail");
    }
    
    #endregion
    
}
