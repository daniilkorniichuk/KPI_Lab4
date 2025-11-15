using Xunit;
using Moq;
using Order_Project.Models;
using Order_Project.Services;
using Order_Project.Services.Intefraces;
using System;
using System.Collections.Generic;

namespace Order_Project_Tests
{
    public class OrderServiceTests
    {
        private readonly Mock<IInventoryService> _inventoryMock;
        private readonly Mock<IPaymentService> _paymentMock;
        private readonly Mock<INotificationService> _notificationMock;
        private readonly OrderService _service;

        public OrderServiceTests()
        {
            _inventoryMock = new Mock<IInventoryService>();
            _paymentMock = new Mock<IPaymentService>();
            _notificationMock = new Mock<INotificationService>();

            _service = new OrderService(_inventoryMock.Object, _paymentMock.Object, _notificationMock.Object);
        }

        ///summary 
        ///(Assert.NotNull, Assert.Equal, Assert.True)
        [Fact]
        public void CreateOrder_Successful_ReturnsOrderAndIsPaid()
        {
            // Arrange
            _inventoryMock.Setup(inv => inv.CheckStock(It.IsAny<string>(), It.IsAny<int>()))
                          .Returns(true);

            _paymentMock.Setup(pay => pay.ProcessPayment(It.IsAny<Order>()))
                        .Returns(true);
            // Act
            var order = _service.CreateOrder("Laptop", 1);

            // Assert 
            Assert.NotNull(order);
            Assert.Equal("Laptop", order.Product);
            Assert.True(order.IsPaid);
        }
        /// <summary>
        /// (Assert.Throws, ArgumentException)
        [Fact]
        public void CreateOrder_QuantityZero_ThrowsArgumentException()
        {
            // Arrange
            string product = "Phone";
            int quantity = 0;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _service.CreateOrder(product, quantity));
        }

        /// <summary>
        /// (Assert.Throws, InvalidOperationException)
        [Fact]
        public void CreateOrder_OutOfStock_ThrowsInvalidOperationException()
        {
            // Arrange
            _inventoryMock.Setup(inv => inv.CheckStock("TV", 1)).Returns(false);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _service.CreateOrder("TV", 1));
        }

        /// <summary>
        /// (Assert.Throws, InvalidOperationException)
        /// </summary>
        [Fact]
        public void CreateOrder_PaymentFails_ThrowsInvalidOperationException()
        {
            // Arrange
            _inventoryMock.Setup(inv => inv.CheckStock(It.IsAny<string>(), It.IsAny<int>()))
                          .Returns(true);
            _paymentMock.Setup(pay => pay.ProcessPayment(It.IsAny<Order>()))
                        .Returns(false);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _service.CreateOrder("Mouse", 1));
        }

        /// <summary>
        /// (Verify, Times.Once, It.IsAny)
        [Fact]
        public void CreateOrder_Successful_CallsDependenciesOnce()
        {
            // Arrange
            _inventoryMock.Setup(inv => inv.CheckStock(It.IsAny<string>(), It.IsAny<int>())).Returns(true);
            _paymentMock.Setup(pay => pay.ProcessPayment(It.IsAny<Order>())).Returns(true);

            // Act
            _service.CreateOrder("Keyboard", 1);

            // Assert
            _inventoryMock.Verify(inv => inv.ReduceStock(It.IsAny<string>(), It.IsAny<int>()), Times.Once);

            _notificationMock.Verify(n => n.SendConfirmation(It.IsAny<Order>()), Times.Once);
        }

        /// <summary>
        /// (Verify, Times.Never)
        [Fact]
        public void CreateOrder_PaymentFails_DoesNotSendConfirmation()
        {
            // Arrange
            _inventoryMock.Setup(inv => inv.CheckStock(It.IsAny<string>(), It.IsAny<int>())).Returns(true);
            _paymentMock.Setup(pay => pay.ProcessPayment(It.IsAny<Order>())).Returns(false);

            // Act
            try
            {
                _service.CreateOrder("Mic", 1);
            }
            catch (InvalidOperationException)
            {
            }

            // Assert
            _notificationMock.Verify(n => n.SendConfirmation(It.IsAny<Order>()), Times.Never);
        }

        /// <summary>
        /// (Verify, Times.AtLeastOnce)
        [Fact]
        public void CreateOrder_PaymentFails_RestocksInventory()
        {
            // Arrange
            _inventoryMock.Setup(inv => inv.CheckStock(It.IsAny<string>(), It.IsAny<int>())).Returns(true);
            _paymentMock.Setup(pay => pay.ProcessPayment(It.IsAny<Order>())).Returns(false);

            // Act
            Assert.Throws<InvalidOperationException>(() => _service.CreateOrder("Webcam", 2));

            // Assert
            _inventoryMock.Verify(inv => inv.ReduceStock("Webcam", 2), Times.Once);
            _inventoryMock.Verify(inv => inv.IncreaseStock("Webcam", 2), Times.AtLeastOnce);
        }

        /// <summary>
        /// (Assert.False)
        [Fact]
        public void UpdateOrder_NonExistingOrder_ReturnsFalse()
        {
            // Arrange
            int nonExistingId = 999;

            // Act
            bool result = _service.UpdateOrder(nonExistingId, 5);

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// (Assert.True, Assert.Equal)
        [Fact]
        public void UpdateOrder_ExistingOrder_ReturnsTrueAndUpdatesQuantity()
        {
            // Arrange
            _inventoryMock.Setup(inv => inv.CheckStock(It.IsAny<string>(), It.IsAny<int>())).Returns(true);
            _paymentMock.Setup(pay => pay.ProcessPayment(It.IsAny<Order>())).Returns(true);
            var order = _service.CreateOrder("Monitor", 1);

            // Act
            bool result = _service.UpdateOrder(order.Id, 3);

            // Assert
            Assert.True(result);
            Assert.Equal(3, order.Quantity);
        }

        /// <summary>
        /// ([Theory], [InlineData])
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public void UpdateOrder_InvalidNewQuantity_ReturnsFalse(int invalidQuantity)
        {
            // Arrange
            _inventoryMock.Setup(inv => inv.CheckStock(It.IsAny<string>(), It.IsAny<int>())).Returns(true);
            _paymentMock.Setup(pay => pay.ProcessPayment(It.IsAny<Order>())).Returns(true);
            var order = _service.CreateOrder("Monitor", 1);

            // Act
            bool result = _service.UpdateOrder(order.Id, invalidQuantity);

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// (Assert.NotEmpty, Assert.Empty)
        [Fact]
        public void RemoveOrder_ExistingOrder_ReturnsTrueAndRemovesFromList()
        {
            // Arrange
            _inventoryMock.Setup(inv => inv.CheckStock(It.IsAny<string>(), It.IsAny<int>())).Returns(true);
            _paymentMock.Setup(pay => pay.ProcessPayment(It.IsAny<Order>())).Returns(true);
            var order = _service.CreateOrder("Desk", 1);

            Assert.NotEmpty(_service.GetOrders());

            // Act
            bool result = _service.RemoveOrder(order.Id);

            // Assert
            Assert.True(result);
            Assert.Empty(_service.GetOrders());
        }

        /// <summary>
        /// (Assert.False)
        [Fact]
        public void RemoveOrder_NonExistingOrder_ReturnsFalse()
        {
            // Act
            bool result = _service.RemoveOrder(123);

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// (Verify, Times.Exactly)
        [Fact]
        public void RemoveOrder_ExistingOrder_CallsIncreaseStock()
        {
            // Arrange
            _inventoryMock.Setup(inv => inv.CheckStock(It.IsAny<string>(), It.IsAny<int>())).Returns(true);
            _paymentMock.Setup(pay => pay.ProcessPayment(It.IsAny<Order>())).Returns(true);
            var order = _service.CreateOrder("Chair", 5);

            // Act
            _service.RemoveOrder(order.Id);

            // Assert
            _inventoryMock.Verify(inv => inv.IncreaseStock("Chair", 5), Times.Exactly(1));
        }

        /// <summary>
        /// (Assert.NotEqual, Assert.Contains)
        [Fact]
        public void CreateOrder_TwoOrders_HaveDifferentIds()
        {
            // Arrange
            _inventoryMock.Setup(inv => inv.CheckStock(It.IsAny<string>(), It.IsAny<int>())).Returns(true);
            _paymentMock.Setup(pay => pay.ProcessPayment(It.IsAny<Order>())).Returns(true);

            // Act
            var order1 = _service.CreateOrder("Item A", 1);
            var order2 = _service.CreateOrder("Item B", 2);

            // Assert
            Assert.NotEqual(order1.Id, order2.Id);

            var allOrders = _service.GetOrders();
            Assert.Contains(order1, allOrders);
            Assert.Contains(order2, allOrders);
        }

        /// <summary>
        /// (It.Is)
        [Fact]
        public void CreateOrder_Successful_CallsPaymentWithCorrectOrder()
        {
            // Arrange
            _inventoryMock.Setup(inv => inv.CheckStock(It.IsAny<string>(), It.IsAny<int>())).Returns(true);
            _paymentMock.Setup(pay => pay.ProcessPayment(It.IsAny<Order>())).Returns(true);

            // Act
            _service.CreateOrder("HighValueItem", 10);

            // Assert
            _paymentMock.Verify(pay => pay.ProcessPayment(
                It.Is<Order>(o => o.Product == "HighValueItem" && o.Quantity > 5)
            ), Times.Once);
        }
    }
}
