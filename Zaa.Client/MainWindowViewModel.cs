using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Mvvm;
using Zza.Client.ZzaServices;
using Zza.Entities;


namespace Zza.Client
{
    public class MainWindowViewModel : BindableBase
    {
        private ObservableCollection<Product> _products;
        private ObservableCollection<Customer> _customers;
        private ObservableCollection<OrderItemModel> _items = new ObservableCollection<OrderItemModel>();
        private Order _currentOrder = new Order();

        public MainWindowViewModel()
        {
            _currentOrder.OrderDate = DateTime.Now;
            _currentOrder.OrderStatusId = 1;
            SubmitOrderCommand = new DelegateCommand(OnSubmitOrder);
            AddOrderItemCommand = new DelegateCommand<Product>(OnAddItem);
            if (!DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                LoadProductsAndCustomers();
            }
        }

        public ObservableCollection<Product> Products
        {
            get { return _products; }
            set { SetProperty(ref _products, value); }
        }

        public ObservableCollection<Customer> Customers
        {
            get { return _customers; }
            set { SetProperty(ref _customers, value); }
        }

        public ObservableCollection<OrderItemModel> Items
        {
            get { return _items; }
            set { SetProperty(ref _items, value); }
        }

        public Order CurrentOrder
        {
            get { return _currentOrder; }
            set { SetProperty(ref _currentOrder, value); }
        }

        public DelegateCommand SubmitOrderCommand { get; private set; }
        public DelegateCommand<Product> AddOrderItemCommand { get; private set; }

        private void OnAddItem(Product product)
        {
            var existingOrderItem = _currentOrder.OrderItems.Where(oi => oi.ProductId == product.Id).FirstOrDefault();
            var existingOrderItemModel = _items.Where(i => i.ProductId == product.Id).FirstOrDefault();
            if (existingOrderItem != null && existingOrderItemModel != null)
            {
                existingOrderItem.Quantity++;
                existingOrderItemModel.Quantity++;
                existingOrderItem.TotalPrice = existingOrderItem.UnitPrice * existingOrderItem.Quantity;
                existingOrderItemModel.TotalPrice = existingOrderItem.TotalPrice;
            }
            else
            {
                var orderItem = new OrderItem { ProductId = product.Id, Quantity = 1, UnitPrice = 9.99m, TotalPrice = 9.99m };
                _currentOrder.OrderItems.Add(orderItem);
                Items.Add(new OrderItemModel { ProductId = product.Id, ProductName = product.Name, Quantity = orderItem.Quantity, TotalPrice = orderItem.TotalPrice });
            }

        }

        private async void LoadProductsAndCustomers()
        {
            ZzaServiceClient proxy = new ZzaServiceClient("NetTcpBinding_IZzaService");

            //proxy.ClientCredentials.Windows.ClientCredential.UserName = "xxx\\test";
            //proxy.ClientCredentials.Windows.ClientCredential.Password = "test";//get it from user through popup login

            try
            {
                Products = await proxy.GetProductsAsync();
                Customers = await proxy.GetCustomersAsync();

            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load server data. " + ex.Message);
            }
            finally
            {
                proxy.Close();
            }

        }

        private void OnSubmitOrder()
        {
            if (_currentOrder.CustomerId != Guid.Empty && _currentOrder.OrderItems.Count > 0)
            {
                ZzaProxy proxy = new ZzaProxy("NetTcpBinding_IZzaService");
                //ZzaServiceClient proxy = new ZzaServiceClient("NetTcpBinding_IZzaService");
                try
                {
                    proxy.SubmitOrder(_currentOrder);
                    CurrentOrder = new Order();
                    CurrentOrder.OrderDate = DateTime.Now;
                    CurrentOrder.OrderStatusId = 1;
                    Items = new ObservableCollection<OrderItemModel>();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error saving order, please try again later.");
                    // Log it
                }
                finally
                {
                    proxy.Close();
                }
            }
            else
            {
                MessageBox.Show("You must select a customer and add order items to submit an order");
            }
        }
    }
}
