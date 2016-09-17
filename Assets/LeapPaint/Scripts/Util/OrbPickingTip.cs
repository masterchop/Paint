﻿using UnityEngine;
using System.Collections;

public class OrbPickingTip : MonoBehaviour {
  //public WearableAnchor Anchor;
  public WearableManager Manager;
  public WearableUI MenuUI;
  public WearableUI ColorUI;
  public WearableUI BrushUI;
  public TextMesh text;
  public TweenHandle disappearTween;

  private bool hasShownOnce = false;
  private Transform parent;

  LineRenderer firstLine;
  Vector3[] line1 = new Vector3[2];
  float fade = 0f;

  void Start() {
    parent = transform.parent;
    transform.parent = null;

    text.gameObject.SetActive(true);
    Manager.OnGrabBegin += DoOnBeginDisappearing;
    MenuUI.OnActivateMarble += DoOnBeginDisappearing;
    ColorUI.OnActivateMarble += DoOnBeginDisappearing;
    BrushUI.OnActivateMarble += DoOnBeginDisappearing;
    disappearTween = Tween.Value(new Color(0.9f, 0.9f, 0.9f, 1f), new Color(0.9f, 0.9f, 0.9f, 0f), SetOpacity)
      .OverTime(0.3f)
      .Smooth(TweenType.SMOOTH)
      .OnReachEnd(Hide)
      .Keep();

    firstLine = gameObject.AddComponent<LineRenderer>();
    firstLine.useWorldSpace = true;
    firstLine.material = new Material(Shader.Find("Standard"));
    firstLine.SetColors(Color.black, Color.black);
    firstLine.SetPositions(line1);
    firstLine.SetWidth(0.002f, 0.002f);
  }

  void DoOnBeginDisappearing() {
    if (!hasShownOnce) {
      disappearTween.Play();
      hasShownOnce = true;
    }
  }

  void Hide() {
    text.gameObject.SetActive(false);
  }

  // Update is called once per frame
  void Update() {
    transform.localScale = parent.lossyScale;
    if (MenuUI.AnchorChirality == Leap.Unity.Chirality.Left) {
      transform.position = Vector3.Lerp(transform.position, parent.TransformPoint(new Vector3(-2f, 0f, 0.55f)), 0.1f);
      text.anchor = TextAnchor.MiddleLeft;
    } else if (MenuUI.AnchorChirality == Leap.Unity.Chirality.Right) {
      transform.position = Vector3.Lerp(transform.position, parent.TransformPoint(new Vector3(2f, 0f, -0.55f)), 0.1f);
      text.anchor = TextAnchor.MiddleRight;
    }

    transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);

    firstLine.SetPosition(0, Vector3.Lerp(transform.position, ColorUI.transform.position, Mathf.Lerp(0.2f, 0.5f, fade)));
    firstLine.SetPosition(1, Vector3.Lerp(transform.position, ColorUI.transform.position, Mathf.Lerp(0.7f, 0.5f, fade)));
    firstLine.SetWidth(transform.localScale.x * 0.05f, transform.localScale.x * 0.05f);
  }

  public void SetOpacity(Color color) {
    fade = 1f - color.a;
    Material textMat = text.GetComponent<Renderer>().material;
    textMat.color = color;
  }
}