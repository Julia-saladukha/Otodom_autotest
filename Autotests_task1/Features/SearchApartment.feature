Feature: Search apartments on Otodom
  As a user
  I want to search for apartments
  So that I can verify filters and offer details

  Scenario: Verify search filters and offer details
    Given I open Otodom main page
    And I accept cookies if popup appears
    When I authorize user
    And I set location and price filters and search
    Then Search results should display apartments with price in selected range
    When I clear price filter and set surface range from first page
    Then Search results should display apartments with surface in selected range
    When I open random offer and save its details
    Then Offer details should match saved values
