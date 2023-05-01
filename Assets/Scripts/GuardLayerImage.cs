using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HeppokoUtil
{
    /// <summary>
    /// チュートリアルなどで特定の UI 要素の周辺だけを表示させたい場合に使うコンポーネント
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class GuardLayerImage : Image, IPointerClickHandler
    {
        /// <summary>
        /// 表示対象のオブジェクト
        /// </summary>
        [SerializeField]
        private GameObject targetObject;

        /// <summary>
        /// 表示対象のオブジェクトにアタッチされている Image
        /// </summary>
        private Image targetImage;

        /// <summary>
        /// 表示対象の周囲の空白
        /// </summary>
        [SerializeField]
        private Vector2 _margin;
        public Vector2 margin
        {
            get
            {
                return _margin;
            }

            set
            {
                if (_margin != value)
                {
                    _margin = value;
                    SetAllDirty();
                }
            }
        }

        public UnityEvent onClickAction;

        /// <summary>
        /// 表示対象のオブジェクトにアタッチされている Image の Sprite
        /// </summary>
        private Sprite targetSprite;

        /// <summary>
        /// 表示対象のオブジェクトの RectTransform の変更を検出するためのキャッシュ
        /// </summary>
        private RectTransform lastRectTransform;

        /// <summary>
        /// 表示対象のオブジェクトの周囲の左/右/下/上
        /// </summary>
        private float centerL;
        private float centerR;
        private float centerT;
        private float centerB;

        /// <summary>
        /// Canvas に対する Scale
        /// </summary>
        private float scaleX;
        private float scaleY;

        /// <summary>
        /// 初期化したかどうか
        /// </summary>
        private bool initialized;

        /// <summary>
        /// 表示対象のオブジェクトを設定する。
        /// Image であれば Sprite の形で切り抜かれ、そうでなければ RectTransform の矩形で切り抜かれる。
        /// </summary>
        /// <param name="targetObject"></param>
        public void SetTargetObject(GameObject targetObject)
        {
            this.targetObject = targetObject;
            SetAllDirty();
        }

        protected override void Awake()
        {
            base.Awake();

            initialized = false;
        }

        protected override void Start()
        {
            base.Start();

            ResizeRectTransform();
        }

        protected void Update()
        {
            if (!initialized)
            {
                ResizeRectTransform();
                SetAllDirty();
                initialized = true;
            }
            else
            {
                if (targetObject != null && lastRectTransform != targetObject.transform as RectTransform)
                {
                    ResizeRectTransform();
                    SetVerticesDirty();
                    lastRectTransform = targetObject.transform as RectTransform;
                }
            }
        }

        /// <summary>
        /// この RectTransform を Canvas いっぱいにする 
        /// </summary>
        protected void ResizeRectTransform()
        {
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.sizeDelta = new Vector2(0, 0);
            rectTransform.pivot = new Vector2(0, 0);
        }

        protected override void UpdateMaterial()
        {
            if (m_Material == null)
            {
                var shader = Shader.Find("UI/TutorialLayerImage");
                if (shader == null)
                {
                    Debug.LogError("シェーダーが見つかりません");
                    return;
                }
                m_Material = new Material(shader);
            }

            m_Material.SetColor("_Color", color);
            canvasRenderer.materialCount = 1;
            canvasRenderer.SetMaterial(m_Material, 0);

            if (targetImage != null)
            {
                canvasRenderer.SetTexture(targetImage.mainTexture);
            }
        }

        /// <summary>
        /// 頂点を作成する
        /// </summary>
        /// <param name="vh"></param>
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            if (canvas == null)
            {
                return;
            }

            vh.Clear();

            if (targetObject == null)
            {
                return;
            }

            targetImage = targetObject.GetComponent<Image>();

            if (targetImage != null)
            {
                OnPopulateMeshImage(vh);
            }
            else
            {
                OnPopulateMeshRect(vh);
            }
        }

        /// <summary>
        /// 頂点を作成する（対象のオブジェクトが Image ではない場合）
        /// </summary>
        /// <param name="vh"></param>
        protected void OnPopulateMeshRect(VertexHelper vh)
        {
            Vector2 targetPos;

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                targetPos = targetObject.transform.position;
            }
            else
            {
                targetPos = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, targetObject.transform.position);
            }

            var targetRectTransform = targetObject.GetComponent<RectTransform>();
            if (targetRectTransform == null)
            {
                return;
            }

            Rect targetRect = targetRectTransform.rect;
            float fullWidth = rectTransform.rect.size.x;
            float fullHeight = rectTransform.rect.size.y;

            var canvasScaleX = canvas.transform.lossyScale.x;
            var canvasScaleY = canvas.transform.lossyScale.y;
            scaleX = targetRectTransform.lossyScale.x / canvasScaleX;
            scaleY = targetRectTransform.lossyScale.y / canvasScaleY;

            targetPos.x /= canvasScaleX;
            targetPos.y /= canvasScaleY;
            
            float centerWidth = targetRect.size.x * scaleX;
            float centerHeight = targetRect.size.y * scaleY;

            // RectTransform の四隅（＋マージン）の座標
            centerL = targetPos.x - centerWidth * targetRectTransform.pivot.x - margin.x;
            centerR = targetPos.x + centerWidth * (1 - targetRectTransform.pivot.x) + margin.x;
            centerT = targetPos.y + centerHeight * (1 - targetRectTransform.pivot.y) + margin.y;
            centerB = targetPos.y - centerHeight * targetRectTransform.pivot.y - margin.y;

            // 黒半透明色（シェーダー内で Color にする）
            Color emptyColor = new Color(1, 1, 1, 0);

            // 半透明エリアはテクスチャを使わないので全部 UV は (0, 0)
            Vector2 uvzero = new Vector2(0, 0);

            // 左の半透明エリア
            vh.AddVert(new Vector2(0, 0), emptyColor, uvzero);
            vh.AddVert(new Vector2(0, fullHeight), emptyColor, uvzero);
            vh.AddVert(new Vector2(centerL, fullHeight), emptyColor, uvzero);
            vh.AddVert(new Vector2(centerL, 0), emptyColor, uvzero);

            // 右の半透明エリア
            vh.AddVert(new Vector2(centerR, 0), emptyColor, uvzero);
            vh.AddVert(new Vector2(centerR, fullHeight), emptyColor, uvzero);
            vh.AddVert(new Vector2(fullWidth, fullHeight), emptyColor, uvzero);
            vh.AddVert(new Vector2(fullWidth, 0), emptyColor, uvzero);

            // 上の半透明エリア
            vh.AddVert(new Vector2(centerL, centerT), emptyColor, uvzero);
            vh.AddVert(new Vector2(centerL, fullHeight), emptyColor, uvzero);
            vh.AddVert(new Vector2(centerR, fullHeight), emptyColor, uvzero);
            vh.AddVert(new Vector2(centerR, centerT), emptyColor, uvzero);

            // 下の半透明エリア
            vh.AddVert(new Vector2(centerL, 0), emptyColor, uvzero);
            vh.AddVert(new Vector2(centerL, centerB), emptyColor, uvzero);
            vh.AddVert(new Vector2(centerR, centerB), emptyColor, uvzero);
            vh.AddVert(new Vector2(centerR, 0), emptyColor, uvzero);

            for (int i = 0; i < 4; i++)
            {
                vh.AddTriangle(i * 4 + 0, i * 4 + 1, i * 4 + 2);
                vh.AddTriangle(i * 4 + 2, i * 4 + 3, i * 4 + 0);
            }
        }

        /// <summary>
        /// 頂点を作成する（対象のオブジェクトが Image の場合）
        /// </summary>
        /// <param name="vh"></param>
        protected void OnPopulateMeshImage(VertexHelper vh)
        {
            if (targetImage == null)
            {
                return;
            }

            targetSprite = targetImage.overrideSprite;

            if (targetSprite == null)
            {
                return;
            }

            var rectTransform = transform as RectTransform;

            Vector2 targetPos;
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                targetPos = targetObject.transform.position;
            }
            else
            {
                targetPos = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, targetObject.transform.position);
            }

            var targetRectTransform = targetObject.transform as RectTransform;
            Rect targetRect = targetRectTransform.rect;
            float fullWidth = rectTransform.rect.size.x;
            float fullHeight = rectTransform.rect.size.y;
            float imageWidth = targetSprite.rect.width;
            float imageHeight = targetSprite.rect.height;
            float spriteRatio = imageWidth / imageHeight;
            float rectRatio = targetRect.width / targetRect.height;

            var canvasScaleX = canvas.transform.lossyScale.x;
            var canvasScaleY = canvas.transform.lossyScale.y;
            scaleX = targetRectTransform.lossyScale.x / canvasScaleX;
            scaleY = targetRectTransform.lossyScale.y / canvasScaleY;

            targetPos.x /= canvasScaleX;
            targetPos.y /= canvasScaleY;
            
            float centerWidth = targetRect.size.x * scaleX;
            float centerHeight = targetRect.size.y * scaleY;

            if (targetImage.type == Type.Simple && targetImage.preserveAspect)
            {
                if (spriteRatio > rectRatio)
                {
                    centerHeight = centerWidth * (1.0f * spriteRatio);
                }
                else
                {
                    centerWidth = centerHeight * (1.0f * spriteRatio);
                }
            }

            // Image の四隅（＋マージン）の座標
            centerL = targetPos.x - centerWidth * targetRectTransform.pivot.x - margin.x;
            centerR = targetPos.x + centerWidth * (1 - targetRectTransform.pivot.x) + margin.x;
            centerT = targetPos.y + centerHeight * (1 - targetRectTransform.pivot.y) + margin.y;
            centerB = targetPos.y - centerHeight * targetRectTransform.pivot.y - margin.y;

            float paddingL = 0;
            float paddingR = 0;
            float paddingT = 0;
            float paddingB = 0;

            if (targetImage.type == Type.Sliced)
            {
                var padding = UnityEngine.Sprites.DataUtility.GetPadding(targetImage.overrideSprite);
                padding = padding / (targetImage.pixelsPerUnit * targetImage.pixelsPerUnitMultiplier);

                paddingL = padding.x;
                paddingB = padding.y;
                paddingR = padding.z;
                paddingT = padding.w;
            }

            paddingL *= scaleX;
            paddingB *= scaleY;
            paddingR *= scaleX;
            paddingT *= scaleY;

            Vector4 border = targetSprite.border; // x:左 y:下 z:右 w:上
            border = border / (targetImage.pixelsPerUnit * targetImage.pixelsPerUnitMultiplier);

            float borderL = border.x * scaleX;
            float borderB = border.y * scaleY;
            float borderR = border.z * scaleX;
            float borderT = border.w * scaleY;

            // border よりも Image が小さいなら調整する
            float horizontalBorderSum = borderL + borderR;
            if (horizontalBorderSum > targetRect.width && horizontalBorderSum != 0)
            {
                if (targetSprite.packingMode == SpritePackingMode.Rectangle)
                {
                    paddingL *= targetRect.width / horizontalBorderSum;
                    paddingR *= targetRect.width / horizontalBorderSum;
                }
            }

            float verticalBorderSum = borderT + borderB;
            if (verticalBorderSum > targetRect.height && verticalBorderSum != 0)
            {
                if (targetSprite.packingMode == SpritePackingMode.Rectangle)
                {
                    paddingB *= targetRect.height / verticalBorderSum;
                    paddingT *= targetRect.height / verticalBorderSum;
                }
            }

            // 黒半透明色（シェーダー内で Color にする）
            Color emptyColor = new Color(1, 1, 1, 0);

            // 半透明エリアはテクスチャを使わないので全部 UV は (0, 0)
            Vector2 uvzero = new Vector2(0, 0);

            // 左の半透明エリア
            vh.AddVert(new Vector2(0, 0), emptyColor, uvzero);
            vh.AddVert(new Vector2(0, fullHeight), emptyColor, uvzero);
            vh.AddVert(new Vector2(centerL + paddingL, fullHeight), emptyColor, uvzero);
            vh.AddVert(new Vector2(centerL + paddingL, 0), emptyColor, uvzero);

            // 右の半透明エリア
            vh.AddVert(new Vector2(centerR - paddingR, 0), emptyColor, uvzero);
            vh.AddVert(new Vector2(centerR - paddingR, fullHeight), emptyColor, uvzero);
            vh.AddVert(new Vector2(fullWidth, fullHeight), emptyColor, uvzero);
            vh.AddVert(new Vector2(fullWidth, 0), emptyColor, uvzero);

            // 上の半透明エリア
            vh.AddVert(new Vector2(centerL + paddingL, centerT - paddingT), emptyColor, uvzero);
            vh.AddVert(new Vector2(centerL + paddingL, fullHeight), emptyColor, uvzero);
            vh.AddVert(new Vector2(centerR - paddingR, fullHeight), emptyColor, uvzero);
            vh.AddVert(new Vector2(centerR - paddingR, centerT - paddingT), emptyColor, uvzero);

            // 下の半透明エリア
            vh.AddVert(new Vector2(centerL + paddingL, 0), emptyColor, uvzero);
            vh.AddVert(new Vector2(centerL + paddingL, centerB + paddingB), emptyColor, uvzero);
            vh.AddVert(new Vector2(centerR - paddingR, centerB + paddingB), emptyColor, uvzero);
            vh.AddVert(new Vector2(centerR - paddingR, 0), emptyColor, uvzero);

            for (int i = 0; i < 4; i++)
            {
                vh.AddTriangle(i * 4 + 0, i * 4 + 1, i * 4 + 2);
                vh.AddTriangle(i * 4 + 2, i * 4 + 3, i * 4 + 0);
            }

            // 真ん中のエリアの頂点は type によって変わる
            switch (targetImage.type)
            {
                case Type.Simple:
                case Type.Filled:
                    GenerateSimpleSpriteArea(vh);
                    break;

                case Type.Sliced:
                    if (targetSprite.border.sqrMagnitude > 0)
                    {
                        GenerateSlicedSpriteArea(vh);
                    }
                    else
                    {
                        GenerateSimpleSpriteArea(vh);
                    }
                    break;

                case Type.Tiled:
                    GenerateTiledSpriteArea(vh);
                    break;
            }
        }

        // Type が Simple および Filled の場合
        protected void GenerateSimpleSpriteArea(VertexHelper vh)
        {
            // Image の頂点カラーは白をシェーダに渡す
            Color centerColor = new Color(1, 1, 1, 1);

            float uvL = targetSprite.rect.x / targetSprite.texture.width;
            float uvR = (targetSprite.rect.x + targetSprite.rect.width) / targetSprite.texture.width;
            float uvB = targetSprite.rect.y / targetSprite.texture.height;
            float uvT = (targetSprite.rect.y + targetSprite.rect.height) / targetSprite.texture.height;

            // Image の周りのエリア
            vh.AddVert(new Vector2(centerL, centerB), centerColor, new Vector2(uvL, uvB));
            vh.AddVert(new Vector2(centerL, centerT), centerColor, new Vector2(uvL, uvT));
            vh.AddVert(new Vector2(centerR, centerT), centerColor, new Vector2(uvR, uvT));
            vh.AddVert(new Vector2(centerR, centerB), centerColor, new Vector2(uvR, uvB));

            for (int i = 4; i < 5; i++)
            {
                vh.AddTriangle(i * 4 + 0, i * 4 + 1, i * 4 + 2);
                vh.AddTriangle(i * 4 + 2, i * 4 + 3, i * 4 + 0);
            }
        }

        // Type が Sliced の場合
        protected void GenerateSlicedSpriteArea(VertexHelper vh)
        {
            var targetSprite = targetImage.overrideSprite;
            var outerUV = UnityEngine.Sprites.DataUtility.GetOuterUV(targetSprite);
            var innerUV = UnityEngine.Sprites.DataUtility.GetInnerUV(targetSprite);
            var padding = UnityEngine.Sprites.DataUtility.GetPadding(targetSprite);
            var border = targetSprite.border; // x:左 y:下 z:右 w:上

            var imageRectTransform = targetImage.GetComponent<RectTransform>();
            var imageRect = imageRectTransform.rect;

            padding = padding / (targetImage.pixelsPerUnit * targetImage.pixelsPerUnitMultiplier);
            border = border / (targetImage.pixelsPerUnit * targetImage.pixelsPerUnitMultiplier);

            float paddingL = padding.x * scaleX;
            float paddingB = padding.y * scaleY;
            float paddingR = padding.z * scaleX;
            float paddingT = padding.w * scaleY;

            float borderL = border.x * scaleX;
            float borderB = border.y * scaleY;
            float borderR = border.z * scaleX;
            float borderT = border.w * scaleY;
            float border0 = 0;

            // border よりも Image が小さいなら調整する
            float horizontalBorderSum = borderL + borderR;
            if (horizontalBorderSum > imageRect.width && horizontalBorderSum != 0)
            {
                if (targetSprite.packingMode == SpritePackingMode.Rectangle)
                {
                    paddingL *= imageRect.width / horizontalBorderSum;
                    paddingR *= imageRect.width / horizontalBorderSum;
                }
                else
                {
                    borderL *= imageRect.width / horizontalBorderSum;
                    borderR *= imageRect.width / horizontalBorderSum;
                }
            }

            borderL -= paddingL;
            borderR -= paddingR;

            float verticalBorderSum = borderT + borderB;
            if (verticalBorderSum > imageRect.height && verticalBorderSum != 0)
            {
                if (targetSprite.packingMode == SpritePackingMode.Rectangle)
                {
                    paddingB *= imageRect.height / verticalBorderSum;
                    paddingT *= imageRect.height / verticalBorderSum;
                }
                else
                {
                    borderB *= imageRect.height / verticalBorderSum;
                    borderT *= imageRect.height / verticalBorderSum;
                }
            }

            borderB -= paddingB;
            borderT -= paddingT;

            // Image の頂点カラーは白をシェーダに渡す
            Color centerColor = new Color(1, 1, 1, 1);

            // 左下
            vh.AddVert(new Vector2(centerL + paddingL + border0, centerB + paddingB + border0), centerColor, new Vector2(outerUV.x, outerUV.y));
            vh.AddVert(new Vector2(centerL + paddingL + border0, centerB + paddingB + borderB), centerColor, new Vector2(outerUV.x, innerUV.y));
            vh.AddVert(new Vector2(centerL + paddingL + borderL, centerB + paddingB + borderB), centerColor, new Vector2(innerUV.x, innerUV.y));
            vh.AddVert(new Vector2(centerL + paddingL + borderL, centerB + paddingB + border0), centerColor, new Vector2(innerUV.x, outerUV.y));

            // 左中
            vh.AddVert(new Vector2(centerL + paddingL + border0, centerB + paddingB + borderB), centerColor, new Vector2(outerUV.x, innerUV.y));
            vh.AddVert(new Vector2(centerL + paddingL + border0, centerT - paddingT - borderT), centerColor, new Vector2(outerUV.x, innerUV.w));
            vh.AddVert(new Vector2(centerL + paddingL + borderL, centerT - paddingT - borderT), centerColor, new Vector2(innerUV.x, innerUV.w));
            vh.AddVert(new Vector2(centerL + paddingL + borderL, centerB + paddingB + borderB), centerColor, new Vector2(innerUV.x, innerUV.y));

            // 左上
            vh.AddVert(new Vector2(centerL + paddingL + border0, centerT - paddingT - borderT), centerColor, new Vector2(outerUV.x, innerUV.w));
            vh.AddVert(new Vector2(centerL + paddingL + border0, centerT - paddingT - border0), centerColor, new Vector2(outerUV.x, outerUV.w));
            vh.AddVert(new Vector2(centerL + paddingL + borderL, centerT - paddingT - border0), centerColor, new Vector2(innerUV.x, outerUV.w));
            vh.AddVert(new Vector2(centerL + paddingL + borderL, centerT - paddingT - borderT), centerColor, new Vector2(innerUV.x, innerUV.w));

            // 中央下
            vh.AddVert(new Vector2(centerL + paddingL + borderL, centerB + paddingB + border0), centerColor, new Vector2(innerUV.x, outerUV.y));
            vh.AddVert(new Vector2(centerL + paddingL + borderL, centerB + paddingB + borderB), centerColor, new Vector2(innerUV.x, innerUV.y));
            vh.AddVert(new Vector2(centerR - paddingR - borderR, centerB + paddingB + borderB), centerColor, new Vector2(innerUV.z, innerUV.y));
            vh.AddVert(new Vector2(centerR - paddingR - borderR, centerB + paddingB + border0), centerColor, new Vector2(innerUV.z, outerUV.y));

            // 中央中
            vh.AddVert(new Vector2(centerL + paddingL + borderL, centerB + paddingB + borderB), centerColor, new Vector2(innerUV.x, innerUV.y));
            vh.AddVert(new Vector2(centerL + paddingL + borderL, centerT - paddingT - borderT), centerColor, new Vector2(innerUV.x, innerUV.w));
            vh.AddVert(new Vector2(centerR - paddingR - borderR, centerT - paddingT - borderT), centerColor, new Vector2(innerUV.z, innerUV.w));
            vh.AddVert(new Vector2(centerR - paddingR - borderR, centerB + paddingB + borderB), centerColor, new Vector2(innerUV.z, innerUV.y));

            // 中央上
            vh.AddVert(new Vector2(centerL + paddingL + borderL, centerT - paddingT - borderT), centerColor, new Vector2(innerUV.x, innerUV.w));
            vh.AddVert(new Vector2(centerL + paddingL + borderL, centerT - paddingT - border0), centerColor, new Vector2(innerUV.x, outerUV.w));
            vh.AddVert(new Vector2(centerR - paddingR - borderR, centerT - paddingT - border0), centerColor, new Vector2(innerUV.z, outerUV.w));
            vh.AddVert(new Vector2(centerR - paddingR - borderR, centerT - paddingT - borderT), centerColor, new Vector2(innerUV.z, innerUV.w));

            // 右下
            vh.AddVert(new Vector2(centerR - paddingR - borderR, centerB + paddingB + border0), centerColor, new Vector2(innerUV.z, outerUV.y));
            vh.AddVert(new Vector2(centerR - paddingR - borderR, centerB + paddingB + borderB), centerColor, new Vector2(innerUV.z, innerUV.y));
            vh.AddVert(new Vector2(centerR - paddingR - border0, centerB + paddingB + borderB), centerColor, new Vector2(outerUV.z, innerUV.y));
            vh.AddVert(new Vector2(centerR - paddingR - border0, centerB + paddingB + border0), centerColor, new Vector2(outerUV.z, outerUV.y));

            // 右中
            vh.AddVert(new Vector2(centerR - paddingR - borderR, centerB + paddingB + borderB), centerColor, new Vector2(innerUV.z, innerUV.y));
            vh.AddVert(new Vector2(centerR - paddingR - borderR, centerT - paddingT - borderT), centerColor, new Vector2(innerUV.z, innerUV.w));
            vh.AddVert(new Vector2(centerR - paddingR - border0, centerT - paddingT - borderT), centerColor, new Vector2(outerUV.z, innerUV.w));
            vh.AddVert(new Vector2(centerR - paddingR - border0, centerB + paddingB + borderB), centerColor, new Vector2(outerUV.z, innerUV.y));

            // 右上
            vh.AddVert(new Vector2(centerR - paddingR - borderR, centerT - paddingT - borderT), centerColor, new Vector2(innerUV.z, innerUV.w));
            vh.AddVert(new Vector2(centerR - paddingR - borderR, centerT - paddingT - border0), centerColor, new Vector2(innerUV.z, outerUV.w));
            vh.AddVert(new Vector2(centerR - paddingR - border0, centerT - paddingT - border0), centerColor, new Vector2(outerUV.z, outerUV.w));
            vh.AddVert(new Vector2(centerR - paddingR - border0, centerT - paddingT - borderT), centerColor, new Vector2(outerUV.z, innerUV.w));

            for (int i = 4; i < 4 + 9; i++)
            {
                vh.AddTriangle(i * 4 + 0, i * 4 + 1, i * 4 + 2);
                vh.AddTriangle(i * 4 + 2, i * 4 + 3, i * 4 + 0);
            }
        }

        // Type が Tiled の場合
        protected void GenerateTiledSpriteArea(VertexHelper vh)
        {
            var targetSprite = targetImage.overrideSprite;
            var outerUV = UnityEngine.Sprites.DataUtility.GetOuterUV(targetSprite);
            var innerUV = UnityEngine.Sprites.DataUtility.GetInnerUV(targetSprite);
            var padding = UnityEngine.Sprites.DataUtility.GetPadding(targetSprite);
            var border = targetSprite.border; // x:左 y:下 z:右 w:上

            var imageRectTransform = targetImage.GetComponent<RectTransform>();
            var imageRect = imageRectTransform.rect;

            float tileWidth = (targetSprite.rect.width + margin.x * 2 - border.x - border.z) / (targetImage.pixelsPerUnit * targetImage.pixelsPerUnitMultiplier);
            float tileHeight = (targetSprite.rect.height + margin.y * 2 - border.y - border.w) / (targetImage.pixelsPerUnit * targetImage.pixelsPerUnitMultiplier);

            padding = padding / (targetImage.pixelsPerUnit * targetImage.pixelsPerUnitMultiplier);
            border = border / (targetImage.pixelsPerUnit * targetImage.pixelsPerUnitMultiplier);

            float xMin = border.x;
            float xMax = imageRect.width - border.z + margin.x * 2;
            float yMin = border.y;
            float yMax = imageRect.height - border.w + margin.y * 2;

            Vector2 uvMin = new Vector2(innerUV.x, innerUV.y);
            Vector2 uvMax = new Vector2(innerUV.z, innerUV.w);

            int horizontalTilesCount = (int)System.Math.Ceiling((xMax - xMin) / tileWidth);
            int verticalTilesCount = (int)System.Math.Ceiling((yMax - yMin) / tileHeight);

            if (tileWidth <= 0)
            {
                tileWidth = xMax - xMin;
            }

            if (tileHeight <= 0)
            {
                tileHeight = yMax - yMin;
            }

            float paddingL = padding.x * scaleX;
            float paddingB = padding.y * scaleY;
            float paddingR = padding.z * scaleX;
            float paddingT = padding.w * scaleY;

            float borderL = border.x * scaleX;
            float borderB = border.y * scaleY;
            float borderR = border.z * scaleX;
            float borderT = border.w * scaleY;
            float border0 = 0;

            // border よりも Image が小さいなら調整する
            float horizontalBorderSum = borderL + borderR;
            if (horizontalBorderSum > imageRect.width && horizontalBorderSum != 0)
            {
                if (targetSprite.packingMode == SpritePackingMode.Rectangle)
                {
                    paddingL *= imageRect.width / horizontalBorderSum;
                    paddingR *= imageRect.width / horizontalBorderSum;
                }
                else
                {
                    borderL *= imageRect.width / horizontalBorderSum;
                    borderR *= imageRect.width / horizontalBorderSum;
                }
            }

            borderL -= paddingL;
            borderR -= paddingR;

            float verticalBorderSum = borderT + borderB;
            if (verticalBorderSum > imageRect.height && verticalBorderSum != 0)
            {
                if (targetSprite.packingMode == SpritePackingMode.Rectangle)
                {
                    paddingB *= imageRect.height / verticalBorderSum;
                    paddingT *= imageRect.height / verticalBorderSum;
                }
                else
                {
                    borderB *= imageRect.height / verticalBorderSum;
                    borderT *= imageRect.height / verticalBorderSum;
                }
            }

            borderB -= paddingB;
            borderT -= paddingT;

            // Image の頂点カラーは白をシェーダに渡す
            Color centerColor = new Color(1, 1, 1, 1);

            // 半透明エリアはテクスチャを使わないので全部 UV は (0, 0)
            Vector2 uvzero = new Vector2(0, 0);

            // 左下
            vh.AddVert(new Vector2(centerL + paddingL + border0, centerB + paddingB + border0), centerColor, new Vector2(outerUV.x, outerUV.y));
            vh.AddVert(new Vector2(centerL + paddingL + border0, centerB + paddingB + borderB), centerColor, new Vector2(outerUV.x, innerUV.y));
            vh.AddVert(new Vector2(centerL + paddingL + borderL, centerB + paddingB + borderB), centerColor, new Vector2(innerUV.x, innerUV.y));
            vh.AddVert(new Vector2(centerL + paddingL + borderL, centerB + paddingB + border0), centerColor, new Vector2(innerUV.x, outerUV.y));

            // 左上
            vh.AddVert(new Vector2(centerL + paddingL + border0, centerT - paddingT - borderT), centerColor, new Vector2(outerUV.x, innerUV.w));
            vh.AddVert(new Vector2(centerL + paddingL + border0, centerT - paddingT - border0), centerColor, new Vector2(outerUV.x, outerUV.w));
            vh.AddVert(new Vector2(centerL + paddingL + borderL, centerT - paddingT - border0), centerColor, new Vector2(innerUV.x, outerUV.w));
            vh.AddVert(new Vector2(centerL + paddingL + borderL, centerT - paddingT - borderT), centerColor, new Vector2(innerUV.x, innerUV.w));

            // 右下
            vh.AddVert(new Vector2(centerR - paddingR - borderR, centerB + paddingB + border0), centerColor, new Vector2(innerUV.z, outerUV.y));
            vh.AddVert(new Vector2(centerR - paddingR - borderR, centerB + paddingB + borderB), centerColor, new Vector2(innerUV.z, innerUV.y));
            vh.AddVert(new Vector2(centerR - paddingR - border0, centerB + paddingB + borderB), centerColor, new Vector2(outerUV.z, innerUV.y));
            vh.AddVert(new Vector2(centerR - paddingR - border0, centerB + paddingB + border0), centerColor, new Vector2(outerUV.z, outerUV.y));

            // 右上
            vh.AddVert(new Vector2(centerR - paddingR - borderR, centerT - paddingT - borderT), centerColor, new Vector2(innerUV.z, innerUV.w));
            vh.AddVert(new Vector2(centerR - paddingR - borderR, centerT - paddingT - border0), centerColor, new Vector2(innerUV.z, outerUV.w));
            vh.AddVert(new Vector2(centerR - paddingR - border0, centerT - paddingT - border0), centerColor, new Vector2(outerUV.z, outerUV.w));
            vh.AddVert(new Vector2(centerR - paddingR - border0, centerT - paddingT - borderT), centerColor, new Vector2(outerUV.z, innerUV.w));

            Vector2 clippedUV;

            // 上下端
            clippedUV = uvMax;
            for (int i = 0; i < horizontalTilesCount; i++)
            {
                float x1 = xMin + i * tileWidth;
                float x2 = xMin + (i + 1) * tileWidth;
                if (x2 > xMax)
                {
                    clippedUV.x = uvMin.x + (uvMax.x - uvMin.x) * (xMax - x1) / (x2 - x1);
                    x2 = xMax;
                }

                float tileL1 = centerL + paddingL + border0 + x1;
                float tileR1 = centerL + paddingL + border0 + x2;
                float tileB1 = centerB + paddingB + border0 + 0;
                float tileT1 = centerB + paddingB + border0 + yMin;

                vh.AddVert(new Vector2(tileL1, tileB1), centerColor, new Vector2(uvMin.x, outerUV.y));
                vh.AddVert(new Vector2(tileL1, tileT1), centerColor, new Vector2(uvMin.x, uvMin.y));
                vh.AddVert(new Vector2(tileR1, tileT1), centerColor, new Vector2(clippedUV.x, uvMin.y));
                vh.AddVert(new Vector2(tileR1, tileB1), centerColor, new Vector2(clippedUV.x, outerUV.y));

                float tileL2 = centerL + paddingL + border0 + x1;
                float tileR2 = centerL + paddingL + border0 + x2;
                float tileB2 = centerT - paddingT - borderT;
                float tileT2 = centerT - paddingT - border0;

                vh.AddVert(new Vector2(tileL2, tileB2), centerColor, new Vector2(uvMin.x, uvMax.y));
                vh.AddVert(new Vector2(tileL2, tileT2), centerColor, new Vector2(uvMin.x, outerUV.w));
                vh.AddVert(new Vector2(tileR2, tileT2), centerColor, new Vector2(clippedUV.x, outerUV.w));
                vh.AddVert(new Vector2(tileR2, tileB2), centerColor, new Vector2(clippedUV.x, innerUV.w));
            }

            // 左右端
            clippedUV = uvMax;
            for (int j = 0; j < verticalTilesCount; j++)
            {
                float y1 = yMin + j * tileHeight;
                float y2 = yMin + (j + 1) * tileHeight;
                if (y2 > yMax)
                {
                    clippedUV.y = uvMin.y + (uvMax.y - uvMin.y) * (yMax - y1) / (y2 - y1);
                    y2 = yMax;
                }

                float tileL1 = centerL + paddingL + border0 + 0;
                float tileR1 = centerL + paddingL + border0 + xMin;
                float tileB1 = centerB + paddingB + border0 + y1;
                float tileT1 = centerB + paddingB + border0 + y2;

                vh.AddVert(new Vector2(tileL1, tileB1), centerColor, new Vector2(outerUV.x, uvMin.y));
                vh.AddVert(new Vector2(tileL1, tileT1), centerColor, new Vector2(outerUV.x, clippedUV.y));
                vh.AddVert(new Vector2(tileR1, tileT1), centerColor, new Vector2(uvMin.x, clippedUV.y));
                vh.AddVert(new Vector2(tileR1, tileB1), centerColor, new Vector2(uvMin.x, uvMin.y));

                float tileL2 = centerL + paddingL + border0 + xMax;
                float tileR2 = centerL + paddingL + border0 + xMax + xMin;
                float tileB2 = centerB + paddingB + border0 + y1;
                float tileT2 = centerB + paddingB + border0 + y2;

                vh.AddVert(new Vector2(tileL2, tileB2), centerColor, new Vector2(uvMax.x, uvMin.y));
                vh.AddVert(new Vector2(tileL2, tileT2), centerColor, new Vector2(uvMax.x, clippedUV.y));
                vh.AddVert(new Vector2(tileR2, tileT2), centerColor, new Vector2(outerUV.z, clippedUV.y));
                vh.AddVert(new Vector2(tileR2, tileB2), centerColor, new Vector2(outerUV.z, uvMin.y));
            }

            // 真ん中
            clippedUV = uvMax;
            for (int j = 0; j < verticalTilesCount; j++)
            {
                float y1 = yMin + j * tileHeight;
                float y2 = yMin + (j + 1) * tileHeight;

                if (y2 > yMax)
                {
                    clippedUV.y = uvMin.y + (uvMax.y - uvMin.y) * (yMax - y1) / (y2 - y1);
                    y2 = yMax;
                }

                clippedUV.x = uvMax.x;
                for (int i = 0; i < horizontalTilesCount; i++)
                {
                    float x1 = xMin + i * tileWidth;
                    float x2 = xMin + (i + 1) * tileWidth;

                    if (x2 > xMax)
                    {
                        clippedUV.x = uvMin.x + (uvMax.x - uvMin.x) * (xMax - x1) / (x2 - x1);
                        x2 = xMax;
                    }

                    float tileL = centerL + paddingL + border0 + x1;
                    float tileR = centerL + paddingL + border0 + x2;
                    float tileB = centerB + paddingB + border0 + y1;
                    float tileT = centerB + paddingB + border0 + y2;

                    vh.AddVert(new Vector2(tileL, tileB), centerColor, new Vector2(uvMin.x, uvMin.y));
                    vh.AddVert(new Vector2(tileL, tileT), centerColor, new Vector2(uvMin.x, clippedUV.y));
                    vh.AddVert(new Vector2(tileR, tileT), centerColor, new Vector2(clippedUV.x, clippedUV.y));
                    vh.AddVert(new Vector2(tileR, tileB), centerColor, new Vector2(clippedUV.x, uvMin.y));
                }
            }

            for (int i = 4; i < 4 + 4 + verticalTilesCount * horizontalTilesCount + verticalTilesCount * 2 + horizontalTilesCount * 2; i++)
            {
                vh.AddTriangle(i * 4 + 0, i * 4 + 1, i * 4 + 2);
                vh.AddTriangle(i * 4 + 2, i * 4 + 3, i * 4 + 0);
            }
        }

        /// <summary>
        /// Image の周りのエリアだけタッチを通過させる 
        /// </summary>
        /// <param name="screenPoint"></param>
        /// <param name="eventCamera"></param>
        /// <returns></returns>
        public override bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            bool result = true;
            screenPoint.x /= canvas.transform.lossyScale.x;
            screenPoint.y /= canvas.transform.lossyScale.y;
            if (centerL <= screenPoint.x && screenPoint.x <= centerR &&
                centerB <= screenPoint.y && screenPoint.y <= centerT)
            {
                result = false;
            }

            return result;
        }

        /// <summary>
        /// このレイヤーがクリックされた場合の処理
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerClick(PointerEventData eventData)
        {
            onClickAction.Invoke();
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(GuardLayerImage), true)]
    public class GuradLayerImageEditor : UnityEditor.UI.ImageEditor
    {
        private SerializedProperty colorProp;
        private SerializedProperty targetImageProp;
        private SerializedProperty marginProp;
        private SerializedProperty raycastTargetProp;
        private SerializedProperty onClickActionProp;

        protected override void OnEnable()
        {
            base.OnEnable();

            colorProp = serializedObject.FindProperty("m_Color");
            targetImageProp = serializedObject.FindProperty("targetObject");
            marginProp = serializedObject.FindProperty("_margin");
            raycastTargetProp = serializedObject.FindProperty("m_RaycastTarget");
            onClickActionProp = serializedObject.FindProperty("onClickAction");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(colorProp);
            EditorGUILayout.PropertyField(targetImageProp);
            EditorGUILayout.PropertyField(marginProp);
            EditorGUILayout.PropertyField(raycastTargetProp);
            EditorGUILayout.PropertyField(onClickActionProp);

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
