namespace TrafficFineSystem.Shared.Enums;

public enum FineStatus
{
    // Memur cezayı kestiğinde ilk bu durumda başlar
    Yeni = 1,          
    
    // Memur "Onaya Gönder" dediğinde bu duruma geçer (Admin onayı bekler)
    OnayBekliyor = 2,  
    
    // Admin onayladığında (Tahsil edildiğinde)
    Tamamlandi = 3,    
    
    // Admin reddettiğinde (İptal)
    IptalEdildi = 4    
}