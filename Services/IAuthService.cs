using AutoTable.Models;
using System.Threading.Tasks;

namespace AutoTable.Services
{
    public interface IAuthService
    {
        Task<bool> SignInAsync(string email, string password);
        Task<bool> SignUpAsync(string fullName, string email, string password, UserRole role);
    }
}
