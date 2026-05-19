using TMPro;
using UnityEngine;

public class CurrentFlavorBoxUI : MonoBehaviour
{
    [Header("Flavor Value Texts")]
    [SerializeField] private TMP_Text txtSweet;
    [SerializeField] private TMP_Text txtSpicy;
    [SerializeField] private TMP_Text txtSour;
    [SerializeField] private TMP_Text txtUmami;
    [SerializeField] private TMP_Text txtTexture;

    public void SetFlavor(FlavorVector vector)
    {
        if (txtSweet != null)
            txtSweet.text = vector.sweet.ToString();

        if (txtSpicy != null)
            txtSpicy.text = vector.spicy.ToString();

        if (txtSour != null)
            txtSour.text = vector.sour.ToString();

        if (txtUmami != null)
            txtUmami.text = vector.umami.ToString();

        if (txtTexture != null)
            txtTexture.text = vector.texture.ToString();
    }

    public void ClearUI()
    {
        SetFlavor(FlavorVector.Zero);
    }
}