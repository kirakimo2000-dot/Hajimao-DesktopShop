using HajimaoDesktopShop.Domain.Economy;

namespace HajimaoDesktopShop.Application.Business.Employees;

public interface IEmployeeOperationsGateway
{
    bool IsStoreOpen(string storeId);

    bool TryDebitEmployeeExpense(Money amount);
}
