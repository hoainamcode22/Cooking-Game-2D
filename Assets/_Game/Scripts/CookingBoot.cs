using System.Collections;
using UnityEngine;

public class CookingBoot : MonoBehaviour
{
    public CookingSelectionManager selection;
    public LeftPanelRefs leftRefs;

    private IEnumerator Start()
    {
        yield return null;

        if (selection == null || leftRefs == null) yield break;

        selection.RegisterAllLeftCards(
            leftRefs.ingredientsContent,
            leftRefs.seasoningsContent
        );
    }
}