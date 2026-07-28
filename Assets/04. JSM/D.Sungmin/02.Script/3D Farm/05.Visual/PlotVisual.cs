using UnityEngine;

namespace WitchChronicle.IdleFarming
{
    public class PlotVisual : MonoBehaviour
    {
        [Header("State Objects")]
        [SerializeField] private GameObject lockedCover;
        [SerializeField] private GameObject emptySlotMarker;
        [SerializeField] private GameObject sproutModel;
        [SerializeField] private GameObject saplingModel;
        [SerializeField] private Transform matureTreeContainer;

        [Header("Growth Thresholds")]
        [Tooltip("진행률이 이 값보다 낮으면 새싹, 크거나 같으면 자라는 나무.")]
        [SerializeField, Range(0f, 1f)] private float saplingThreshold = 0.5f;

        private GameObject currentMatureTree;
        private SeedData currentSeed;

        public void Refresh(PlotState state, SeedData seed, float progress)
        {
            SetAllOff();

            switch (state)
            {
                case PlotState.Locked:
                    lockedCover.SetActive(true);
                    ClearMatureTree();
                    break;

                case PlotState.Empty:
                    emptySlotMarker.SetActive(true);
                    ClearMatureTree();
                    break;

                case PlotState.Growing:
                    ClearMatureTree();
                    if (progress < saplingThreshold)
                        sproutModel.SetActive(true);
                    else
                        saplingModel.SetActive(true);
                    break;

                case PlotState.ReadyToHarvest:
                    EnsureMatureTree(seed);
                    break;
            }
        }

        private void SetAllOff()
        {
            if (lockedCover) lockedCover.SetActive(false);
            if (emptySlotMarker) emptySlotMarker.SetActive(false);
            if (sproutModel) sproutModel.SetActive(false);
            if (saplingModel) saplingModel.SetActive(false);
        }

        private void EnsureMatureTree(SeedData seed)
        {
            if (seed == null || seed.matureTreePrefab == null) return;

            // 같은 씨앗이면 기존 나무 재활용
            if (currentMatureTree != null && currentSeed == seed) return;

            ClearMatureTree();
            currentSeed = seed;
            currentMatureTree = Instantiate(seed.matureTreePrefab, matureTreeContainer);
            currentMatureTree.transform.localPosition = Vector3.zero;
            currentMatureTree.transform.localRotation = Quaternion.identity;
            currentMatureTree.transform.localScale = Vector3.one;
        }

        private void ClearMatureTree()
        {
            currentSeed = null;
            currentMatureTree = null;
            if (matureTreeContainer == null) return;
            for (int i = matureTreeContainer.childCount - 1; i >= 0; i--)
                Destroy(matureTreeContainer.GetChild(i).gameObject);
        }
    }
}