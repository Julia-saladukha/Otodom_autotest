Feature: Search apartments on Otodom
  As a user
  I want to open the site and authorize
  So that I can verify access to my account

  Scenario: Open main page and authorize user
    Given I open Otodom main page
    And I accept cookies if popup appears
    When I authorize user
    Then Main page should be opened
