﻿using System;
using System.Collections.Generic;
using System.Linq;
using GlobalPayments.Api.Entities;
using GlobalPayments.Api.PaymentMethods;
using GlobalPayments.Api.Services;
using GlobalPayments.Api.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GlobalPayments.Api.Tests.GpApi {
    [TestClass]
    public class GpApiStoredPaymentMethodReportTests : BaseGpApiTests {
        private static string Token;

        private static CreditCardData Card = new CreditCardData {
            Number = "4111111111111111",
            ExpMonth = 12,
            ExpYear = 2025,
            Cvn = "123"
        };

        [ClassInitialize]
        public static void ClassInitialize(TestContext context) {
            ServicesContainer.ConfigureService(new GpApiConfig {
                AppId = APP_ID,
                AppKey = APP_KEY
            });

            try {
                Token = Card.Tokenize();
                Assert.IsTrue(!string.IsNullOrEmpty(Token), "Token could not be generated.");
            }
            catch (GatewayException ex) {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void ReportStoredPaymentMethodDetail() {
            StoredPaymentMethodSummary response = ReportingService.StoredPaymentMethodDetail(Token)
                .Execute();
            Assert.IsNotNull(response);
            Assert.IsTrue(response is StoredPaymentMethodSummary);
            Assert.AreEqual(Token, response.Id);
        }

        [TestMethod]
        public void ReportStoredPaymentMethodDetailWithNonExistentId() {
            string storedPaymentMethodId = $"PMT_{Guid.NewGuid()}";
            try {
                ReportingService.StoredPaymentMethodDetail(storedPaymentMethodId)
                    .Execute();
            }
            catch (GatewayException ex) {
                Assert.AreEqual("RESOURCE_NOT_FOUND", ex.ResponseCode);
                Assert.AreEqual("40118", ex.ResponseMessage);
                Assert.AreEqual($"Status Code: NotFound - PAYMENT_METHODS {storedPaymentMethodId} not found at this /ucp/payment-methods/{storedPaymentMethodId}", ex.Message);
            }
        }

        [TestMethod]
        public void ReportStoredPaymentMethodDetailWithRandomId() {
            string storedPaymentMethodId = Guid.NewGuid().ToString();
            try {
                ReportingService.StoredPaymentMethodDetail(storedPaymentMethodId)
                    .Execute();
            }
            catch (GatewayException ex) {
                Assert.AreEqual("INVALID_REQUEST_DATA", ex.ResponseCode);
                Assert.AreEqual("40213", ex.ResponseMessage);
                Assert.AreEqual($"Status Code: BadRequest - payment_method.id: {storedPaymentMethodId} contains unexpected data", ex.Message);
            }
        }

        [TestMethod]
        public void ReportFindStoredPaymentMethodsPaged_By_Id() {
            PagedResult<StoredPaymentMethodSummary> result = ReportingService.FindStoredPaymentMethodsPaged(1, 25)
                .Where(DataServiceCriteria.StoredPaymentMethodId, Token)
                .Execute();
            Assert.IsNotNull(result?.Results);
            Assert.IsTrue(result.Results is List<StoredPaymentMethodSummary>);
            Assert.IsTrue(result.Results.TrueForAll(r => r.Id == Token));
        }

        [TestMethod]
        public void ReportFindStoredPaymentMethodsPaged_By_RandomId() {
            string storedPaymentMethodId = $"PMT_{Guid.NewGuid()}";

            PagedResult<StoredPaymentMethodSummary> result = ReportingService.FindStoredPaymentMethodsPaged(1, 25)
                .Where(DataServiceCriteria.StoredPaymentMethodId, storedPaymentMethodId)
                .Execute();
            Assert.IsNotNull(result?.Results);
            Assert.AreEqual(0, result?.Results.Count);
        }

        [TestMethod]
        [Ignore]
        // TODO: Reported the the GP API team
        // Endpoint is retrieving not filtered results
        public void ReportFindStoredPaymentMethodsPaged_By_NumberLast4() {
            string numberLast4 = Card.Number.Substring(Card.Number.Length - 4);
            PagedResult<StoredPaymentMethodSummary> result = ReportingService.FindStoredPaymentMethodsPaged(1, 10)
                .Where(DataServiceCriteria.CardNumberLastFour, numberLast4)
                .Execute();
            Assert.IsNotNull(result?.Results);
            Assert.IsTrue(result.Results is List<StoredPaymentMethodSummary>);
            Assert.IsTrue(result.Results.TrueForAll(r => r.CardLast4 == "xxxxxxxxxxxx"+numberLast4));
        }

        [TestMethod]
        [Ignore]
        // TODO: Reported the the GP API team
        // Endpoint is retrieving not filtered results
        public void ReportFindStoredPaymentMethodsPaged_By_NumberLast4_Set0000() {
            string numberLast4 = "0000";
            PagedResult<StoredPaymentMethodSummary> result = ReportingService.FindStoredPaymentMethodsPaged(1, 10)
                .Where(DataServiceCriteria.CardNumberLastFour, numberLast4)
                .Execute();
            Assert.IsNotNull(result?.Results);
            Assert.AreEqual(0, result?.Results.Count);
        }

        [TestMethod]
        public void ReportFindStoredPaymentMethodsPaged_By_Reference() {
            StoredPaymentMethodSummary response = ReportingService.StoredPaymentMethodDetail(Token)
                .Execute();
            Assert.IsNotNull(response?.Reference);

            PagedResult<StoredPaymentMethodSummary> result = ReportingService.FindStoredPaymentMethodsPaged(1, 25)
                .Where(SearchCriteria.ReferenceNumber, response.Reference)
                .Execute();
            Assert.IsNotNull(result?.Results);
            Assert.IsTrue(result.Results is List<StoredPaymentMethodSummary>);
            Assert.IsTrue(result.Results.TrueForAll(r => r.Reference == response.Reference));
        }

        [TestMethod]
        public void ReportFindStoredPaymentMethodsPaged_By_Status() {
            var status = StoredPaymentMethodStatus.Active;
            PagedResult<StoredPaymentMethodSummary> result = ReportingService.FindStoredPaymentMethodsPaged(1, 25)
                .Where(SearchCriteria.StoredPaymentMethodStatus, status)
                .Execute();
            Assert.IsNotNull(result?.Results);
            Assert.IsTrue(result.Results is List<StoredPaymentMethodSummary>);
            Assert.IsTrue(result.Results.TrueForAll(r => r.Status == EnumConverter.GetMapping(Target.GP_API, status)));
        }

        [TestMethod]
        public void ReportFindStoredPaymentMethodsPaged_By_StartDate_And_EndDate() {
            DateTime startDate = DateTime.UtcNow.AddDays(-30);
            DateTime endDate = DateTime.UtcNow.AddDays(-10);
            PagedResult<StoredPaymentMethodSummary> result = ReportingService.FindStoredPaymentMethodsPaged(1, 25)
                .Where(SearchCriteria.StartDate, startDate)
                .And(SearchCriteria.EndDate, endDate)
                .Execute();
            Assert.IsNotNull(result?.Results);
            Assert.IsTrue(result.Results is List<StoredPaymentMethodSummary>);
            Assert.IsTrue(result.Results.TrueForAll(r => r.TimeCreated.Date >= startDate.Date && r.TimeCreated.Date <= endDate.Date));
        }

        [TestMethod]
        public void ReportFindStoredPaymentMethodsPaged_By_StartDate_And_EndDate_CurrentDay() {
            DateTime currentDay = DateTime.UtcNow;

            PagedResult<StoredPaymentMethodSummary> result = ReportingService.FindStoredPaymentMethodsPaged(1, 25)
                .Where(SearchCriteria.StartDate, currentDay)
                .And(SearchCriteria.EndDate, currentDay)
                .Execute();
            Assert.IsNotNull(result?.Results);
            Assert.IsTrue(result.Results is List<StoredPaymentMethodSummary>);
            Assert.IsTrue(result.Results.TrueForAll(r => r.TimeCreated.Date >= currentDay.Date && r.TimeCreated.Date <= currentDay.Date));
        }

        [TestMethod]
        public void ReportFindStoredPaymentMethodsPaged_By_StartLastUpdatedDate_And_EndLastUpdatedDate() {
            DateTime startLastUpdatedDate = DateTime.UtcNow.AddDays(-30);
            DateTime endLastUpdatedDate = DateTime.UtcNow.AddDays(-10);
            PagedResult<StoredPaymentMethodSummary> result = ReportingService.FindStoredPaymentMethodsPaged(1, 25)
                .Where(DataServiceCriteria.StartLastUpdatedDate, startLastUpdatedDate)
                .And(DataServiceCriteria.EndLastUpdatedDate, endLastUpdatedDate)
                .Execute();
            Assert.IsNotNull(result?.Results);
            Assert.IsTrue(result.Results is List<StoredPaymentMethodSummary>);
            //ToDo: There is no way to validate the response data
        }

        [TestMethod]
        public void ReportFindStoredPaymentMethodsPaged_OrderBy_TimeCreated_Ascending() {
            PagedResult<StoredPaymentMethodSummary> result = ReportingService.FindStoredPaymentMethodsPaged(1, 25)
                .OrderBy(StoredPaymentMethodSortProperty.TimeCreated, SortDirection.Ascending)
                .Execute();
            Assert.IsNotNull(result?.Results);
            Assert.IsTrue(result.Results is List<StoredPaymentMethodSummary>);
            Assert.IsTrue(result.Results.SequenceEqual(result.Results.OrderBy(r => r.TimeCreated)));
        }

        [TestMethod]
        public void ReportFindStoredPaymentMethodsPaged_OrderBy_TimeCreated_Descending() {
            PagedResult<StoredPaymentMethodSummary> result = ReportingService.FindStoredPaymentMethodsPaged(1, 25)
                .OrderBy(StoredPaymentMethodSortProperty.TimeCreated, SortDirection.Descending)
                .Execute();
            Assert.IsNotNull(result?.Results);
            Assert.IsTrue(result.Results is List<StoredPaymentMethodSummary>);
            Assert.IsTrue(result.Results.SequenceEqual(result.Results.OrderByDescending(r => r.TimeCreated)));
        }

        [TestMethod]
        public void ReportFindStoredPaymentMethodsPaged_OrderBy_TimeCreated() {
            PagedResult<StoredPaymentMethodSummary> resultAsc = ReportingService.FindStoredPaymentMethodsPaged(1, 25)
                .OrderBy(StoredPaymentMethodSortProperty.TimeCreated, SortDirection.Ascending)
                .Execute();
            Assert.IsNotNull(resultAsc?.Results);
            Assert.IsTrue(resultAsc.Results is List<StoredPaymentMethodSummary>);
            Assert.IsTrue(resultAsc.Results.SequenceEqual(resultAsc.Results.OrderBy(r => r.TimeCreated)));

            PagedResult<StoredPaymentMethodSummary> resultDesc = ReportingService.FindStoredPaymentMethodsPaged(1, 25)
                .OrderBy(StoredPaymentMethodSortProperty.TimeCreated, SortDirection.Descending)
                .Execute();
            Assert.IsNotNull(resultDesc?.Results);
            Assert.IsTrue(resultDesc.Results is List<StoredPaymentMethodSummary>);
            Assert.IsTrue(resultDesc.Results.SequenceEqual(resultDesc.Results.OrderByDescending(r => r.TimeCreated)));

            Assert.IsFalse(resultAsc.Results.SequenceEqual(resultDesc.Results));
        }

        //[ClassCleanup]
        // The used credentials have not permissions to delete a tokenized card
        public static void Cleanup() {

            ServicesContainer.ConfigureService(new GpApiConfig
            {
                AppId = APP_ID,
                AppKey = APP_KEY
            });

            CreditCardData tokenizedCard = new CreditCardData {
                Token = Token
            };
            Assert.IsTrue(tokenizedCard.DeleteToken());
        }
    }
}