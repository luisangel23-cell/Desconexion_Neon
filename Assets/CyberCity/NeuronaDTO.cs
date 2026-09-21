[System.Serializable]
public class NeuronaDTO 
{
    public string id;
    public int toxicidad;
    public string estado; // Ej: "PELIGRO_LL"
    public bool encriptado;
    public NeuronaDTO izq;
    public NeuronaDTO der;
}