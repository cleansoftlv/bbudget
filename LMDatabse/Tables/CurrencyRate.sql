CREATE TABLE [dbo].[CurrencyRate]
(
    [rate_date] DATE NOT NULL,                 -- Date the rate applies to (UTC)
    [base_currency] CHAR(3) NOT NULL,          -- ISO 4217 code of base currency
    [quote_currency] CHAR(3) NOT NULL,         -- ISO 4217 code of quote currency
    [rate] DECIMAL(18,8) NOT NULL
);
GO

CREATE UNIQUE INDEX [UX_CurrencyRate_DatePairBudget]
ON [dbo].[CurrencyRate]([rate_date],[base_currency],[quote_currency]);
GO

-- Query helpers
CREATE INDEX [IX_CurrencyRate_BaseQuoteDate] ON [dbo].[CurrencyRate]([base_currency],[quote_currency],[rate_date]);
GO
