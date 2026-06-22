using LilyMarket.Application.Interfaces;
using LilyMarket.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LilyMarket.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    //сохранить все изменения в базе
    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }

    //выполнить действие внутри транзакции
    //если что-то пошло не так — все изменения откатятся
    public async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct = default)
    {
        //начинаем транзакцию на уровне базы данных
        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            //выполняем переданное действие
            await action();
            //если дошли сюда без исключений — фиксируем изменения
            await transaction.CommitAsync(ct);
        }
        catch
        {
            //любое исключение — откатываем всё что успели сделать
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}