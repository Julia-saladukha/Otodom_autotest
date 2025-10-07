Feature: Search apartments on Otodom
  As a user
  I want to open the site and authorize
  So that I can verify access to my account

  Scenario: Open main page, authorize user and search with filters
    Given I open Otodom main page
    And I accept cookies if popup appears
    When I authorize user
    And I set location 'Warszawa' and price range 200000-1000000 and search
    Then I should see the search results for 'Warszawa' within the price range 200000-1000000
    And the search results should be valid
    When I analyze surface area from results and apply surface filter
    And I click search button
    Then the search results page should be displayed
    And the search results should be valid
    #And search results should contain apartments with surface area filters applied
    #When I get random offer and save price, number of rooms, and surface to scenario context
    #And I click on the offer
    #Then the offer page should be opened
    #And the price, number of rooms, and surface should be correct
