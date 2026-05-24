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

    private FlavorVector currentFlavor = FlavorVector.Zero;

    public void SetFlavor(FlavorVector vector)
    {
        currentFlavor = vector;

        if (txtSweet != null)
            txtSweet.text = currentFlavor.sweet.ToString();

        if (txtSpicy != null)
            txtSpicy.text = currentFlavor.spicy.ToString();

        if (txtSour != null)
            txtSour.text = currentFlavor.sour.ToString();

        if (txtUmami != null)
            txtUmami.text = currentFlavor.umami.ToString();

        if (txtTexture != null)
            txtTexture.text = currentFlavor.texture.ToString();
    }
}