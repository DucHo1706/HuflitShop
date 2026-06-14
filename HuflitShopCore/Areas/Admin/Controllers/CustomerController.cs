using HuflitShopCore.DTOs;
using HuflitShopCore.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace HuflitShopCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CustomerController : Controller
    {
        private readonly CustomerService _customerService;

        public CustomerController(CustomerService customerService)
        {
            _customerService = customerService;
        }

        public async Task<IActionResult> Index()
        {
            var customers = await _customerService.GetAllCustomersAsync();
            return View(customers);
        }

        public async Task<IActionResult> Details(string id)
        {
            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null) return NotFound();
            return View(customer);
        }

        [HttpPost]
        public async Task<IActionResult> Disable(string id)
        {
            await _customerService.ToggleCustomerStatusAsync(id, false);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> AvailableCustomer(string id)
        {
            await _customerService.ToggleCustomerStatusAsync(id, true);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> OrderView(string id)
        {
            var orders = await _customerService.GetCustomerOrdersAsync(id);
            ViewBag.CustomerId = id;
            return View(orders);
        }
    }
}