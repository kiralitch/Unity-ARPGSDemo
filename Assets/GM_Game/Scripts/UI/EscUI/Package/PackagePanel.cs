using Game.Task.UI;
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
    private Transform UIMenuTask;
    [SerializeField]
    private Transform UICloseBtn;
    [SerializeField]
    private Transform UIRightCenter;
    [SerializeField]
    private Transform UITaskPanel;
    [SerializeField]
    private Transform UIScrollView;
    [SerializeField]
    private Transform UITaskScrollView;
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

    private TaskLogView TaskLog;
    
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
        PackageLocalItem localItem = GameUIManager.GetInstance.GetPackageLocalItemByUID(chooseItemUid);
        //刷新
        UIDetailPanel.GetComponent<PackageDetailPanel>().RefreshDetailPanel(localItem, this);
        //取消之前选中图片
        FreshLastItemSelectImage();
    }

    //将过去选中的特效删除
    private void FreshLastItemSelectImage()
    {
        if (LastChooseItemUid == null) return;
        PackageLocalItem LastChooseItem = GameUIManager.GetInstance.GetPackageLocalItemByUID(LastChooseItemUid);

        var ScrollContext = UIScrollView.GetComponent<ScrollRect>().content; //获取滚动条context父节点
        for (int i = 0; i < ScrollContext.childCount; i++)
        {
            var ItemCell = ScrollContext.GetChild(i);
            ItemCell.GetComponent<PackageCell>().SetLastSelectToZero(LastChooseItem);
        }
    }
    #endregion

    #region 缓存当前选中菜单
    
    private int CacheUIMenuChild = 0; //上一个孩子菜单
    private string CacheMeusPanelName; //上一个菜单名称
    
    //获取上一个菜单选择
    public void RefreshMenuUI()
    {
        for (int i = 0 ; i < UIMenu.childCount; i++)
        {
            var child = UIMenu.GetChild(i);
            var cell = child.GetComponent<PackageMeusCell>();
            //Debug.Log($"Child[{i}] = {child.name}, 组件 = {(cell != null ? "有" : "无")}, 子物体数 = {child.childCount}");
            
            if(cell == null) continue;

            if (cell.RefreshUI(this))
            {
                CacheUIMenuChild = i;
            }
        }
    }

    //将上一个选中的菜单设为false
    public void SetLastMenuSelectToFalse()
    {
        var child = UIMenu.GetChild(CacheUIMenuChild);
        var LastMeusCell = child.GetComponent<PackageMeusCell>();
        
        if (LastMeusCell == null) return;

        LastMeusCell.SetSelectActive(false);
    }
    
    //任务刷新面板
    private void RefreshTaskScroll()
    {
        //首先清除所有任务面板
        TaskLog.ClearItems();

        //没有初始化任务系统时，直接返回（空列表）
        if (Game.Task.Core.TaskManager.Instance == null) return;

        //根据序列号排序后重新创建并初始化每一个任务格子
        var tasks = Game.Task.Core.TaskManager.Instance.GetSortedLogTasks();

        foreach (var taskInstance in tasks)
        {
            //实例化任务格子的预制体，并挂到 Content 下
            TaskLogItemView item = TaskLog.CreateItem();
            if (item == null) continue;

            //刷新
            item.Refresh(taskInstance, Game.Task.Core.TaskManager.Instance.Database);
        }
    }

    #endregion
    
    protected override void Awake()
    {
        base.Awake();
        InitUI();
        InitClick();
        InitTaskUI();
    }

    private void Start()
    {
        RefreshUI();
        RefreshMenuUI();
    }

    private void RefreshUI()
    {
        RefreshScroll();
        RefreshTaskScroll();
    }

    private void InitTaskUI()
    {
        TaskLog = UITaskScrollView.GetComponent<TaskLogView>();
    }
    
    private void InitUI()
    {
        UIDelectPanel.gameObject.SetActive(false);
        UIBottomMenus.gameObject.SetActive(true);
        UIRightCenter.gameObject.SetActive(true);
        UITaskPanel.gameObject.SetActive(false);
    }

    /* 初始化点击事件 */
    private void InitClick()
    {
        UIMenuPackage.GetComponent<Button>().onClick.AddListener(OnClickWeapon);
        UIMenuFood.GetComponent<Button>().onClick.AddListener(OnClickFood);
        UIMenuTask.GetComponent<Button>().onClick.AddListener(OnClickTask);
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
        foreach (var packageLocalData in GameUIManager.GetInstance.GetSortPackageLocalData())
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

        OpenWeaponMeus();
    }

    private void OpenWeaponMeus()
    {
        if (UIRightCenter.gameObject.activeSelf) return;
        
        UITaskPanel.gameObject.SetActive(false);
        UIRightCenter.gameObject.SetActive(true);
    }
    
    private void OnClickFood()
    {
        print(">OnClickFood");
    }
    
    private void OnClickTask()
    {
        print(">OnClickTask");

        OpenTaskMeus();
    }

    private void OpenTaskMeus()
    {
        if (UITaskPanel.gameObject.activeSelf) return;
        
        UIRightCenter.gameObject.SetActive(false);
        UITaskPanel.gameObject.SetActive(true);
    }

    private void OnClickClose()
    {
        print(">OnClickClose");
        ClosePanel();
        
        Player.Instance.SetUIInputAllMethon(true);
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
