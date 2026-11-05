
using Intalnirea1.Data;
using Intalnirea1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

public class OrdersController : Controller
{
    private readonly LibraryContext _context;

    public OrdersController(LibraryContext context)
    {
        _context = context;
    }

    // GET: ORDERS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Order.ToListAsync());
    }

    // GET: ORDERS/Details/5
    public async Task<IActionResult> Details(int? orderid)
    {
        if (orderid == null)
        {
            return NotFound();
        }

        var order = await _context.Order
            .FirstOrDefaultAsync(m => m.OrderID == orderid);
        if (order == null)
        {
            return NotFound();
        }

        return View(order);
    }

    // GET: ORDERS/Create

    // GET: ORDERS/Create
    public IActionResult Create()
    {
        ViewData["CustomerID"] = new SelectList(
            _context.Customer, "ID", "FullName");

        ViewData["BookID"] = new SelectList(
            _context.Book, "ID", "Title");

        return View(new OrderCreateViewModel());
    }

    // POST: ORDERS/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OrderCreateViewModel vm)
    {
        if (ModelState.IsValid)
        {
            var order = new Order
            {
                CustomerID = vm.CustomerID,
                BookID = vm.BookID,
                OrderDate = vm.OrderDate
            };

            _context.Order.Add(order);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        ViewData["CustomerID"] = new SelectList(
            _context.Customer, "ID", "FullName", vm.CustomerID);

        ViewData["BookID"] = new SelectList(
            _context.Book, "ID", "Title", vm.BookID);

        return View(vm);
    }


    // POST: ORDERS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("OrderID,CustomerID,BookID,OrderDate,Customer,Book")] Order order)
    {
        if (ModelState.IsValid)
        {
            _context.Add(order);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(order);
    }

    // GET: ORDERS/Edit/5
    public async Task<IActionResult> Edit(int? orderid)
    {
        if (orderid == null)
        {
            return NotFound();
        }

        var order = await _context.Order.FindAsync(orderid);
        if (order == null)
        {
            return NotFound();
        }
        return View(order);
    }

    // POST: ORDERS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? orderid, [Bind("OrderID,CustomerID,BookID,OrderDate,Customer,Book")] Order order)
    {
        if (orderid != order.OrderID)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(order);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OrderExists(order.OrderID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(order);
    }

    // GET: ORDERS/Delete/5
    public async Task<IActionResult> Delete(int? orderid)
    {
        if (orderid == null)
        {
            return NotFound();
        }

        var order = await _context.Order
            .FirstOrDefaultAsync(m => m.OrderID == orderid);
        if (order == null)
        {
            return NotFound();
        }

        return View(order);
    }

    // POST: ORDERS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? orderid)
    {
        var order = await _context.Order.FindAsync(orderid);
        if (order != null)
        {
            _context.Order.Remove(order);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool OrderExists(int? orderid)
    {
        return _context.Order.Any(e => e.OrderID == orderid);
    }
}
