using System.Threading.Tasks;

namespace DAL.Sales.HydroSales
{
    public interface IHydroSales
    {
        Task<PostPaymentResponse> PostPaymentAsync(PostPaymentRequest request, int userId);
        Task<GenerateMemoResponse> GenerateMemoAsync(GenerateMemoRequest request, int userId);
    }
}
