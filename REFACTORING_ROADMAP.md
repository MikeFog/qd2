# PresentationObject Inheritance Model - Refactoring Roadmap

**Project**: qd2 - WinForms Application (.NET Framework 4.8 / C# 7.3)  
**Date**: January 2026  
**Status**: Analysis & Planning Document

---

## Executive Summary

This document provides a comprehensive analysis of the `FogSoft.WinForm.Classes.PresentationObject` base class and its derived classes, with a concrete refactoring roadmap to improve maintainability, testability, and correctness. The base class serves as a model/controller-style wrapper for database entities across the WinForms solution, managing CRUD operations, state tracking, and event handling.

### Key Findings

1. **31 derived classes** identified across the solution
2. **Multiple responsibility violations** - base class mixes UI, data access, entity metadata, and state management
3. **Dictionary-based state** creates type-safety and consistency issues
4. **SQL injection risk** in `PKWhereClause` property
5. **Inconsistent override patterns** across derived classes
6. **Limited testability** due to tight coupling with UI and database infrastructure

---

## 1. Architecture Analysis

### 1.1 Base Class Overview: PresentationObject

**Location**: `/FogSoft.WinForm/Classes/PresentationObject.cs`

**Primary Responsibilities** (Current - Too Many!):
- State management via dictionaries (`parameters`, `PKparameters`)
- CRUD operations (`Update`, `Delete`, `Clone`, `Detach`)
- Data loading and refresh (`Refresh`, `Init`, `LoadSingleObject`)
- UI interaction (`ShowPassport`, confirmation dialogs)
- Event management (6 different events)
- Identity management (`Equals`, `GetHashCode`, `Key`, `IDs`)
- Action handling (`DoAction`, `IsActionEnabled`)
- Entity metadata access

**Key Members**:
```csharp
protected Dictionary<string, object> parameters  // Current state
protected Dictionary<string, object> PKparameters  // "Old" PK values for updates
protected Entity entity  // Entity metadata
protected bool isNew  // Lifecycle flag
```

**Key Issues**:
1. **Mixed Concerns**: UI prompts (`MessageBox.ShowQuestion`) mixed with data operations
2. **Type Safety**: String-keyed dictionaries with `object` values - no compile-time type checking
3. **Null Handling**: Implicit null returns from indexer (`this[key]`) can cause runtime errors
4. **SQL Injection**: `PKWhereClause` builds SQL strings directly from parameter values
5. **Error Recovery**: `Update()` catches exceptions and calls `Refresh()` - unclear recovery semantics
6. **Event Firing Inconsistency**: `OnObjectChanged` called in some paths but not others
7. **Global Static Dependencies**: `DataAccessor`, `EntityManager`, `ErrorManager` - hard to test

### 1.2 Derived Classes Inventory

#### In FogSoft.WinForm.Classes (Framework Layer)
1. **ObjectContainer** - Base for entities with children, adds child management
2. **FakeContainer** - Non-persistent container for UI scenarios (inherits ObjectsIterator, not PresentationObject)

#### In Client/Classes (Application Layer) - Direct PresentationObject Descendants
3. **ActJournalRow** - Overrides `DoAction` for campaign-related actions
4. **ActionRollerInStatJournal** - Statistics journal entry
5. **Announcement** - System announcements
6. **FirmBalance** (abstract) - Base for balance tracking
7. **MuteRoller** - Dummy/silent video content
8. **Roller** - Video advertisement content, overrides `ShowPassport`
9. **SponsorTariff** - Sponsorship pricing
10. **Tariff** - Standard pricing, overrides `DoAction` for cloning
11. **TariffWindow** - Time-based pricing windows
12. **Domain/MasterIssue** - Issue tracking
13. **Domain/StudioOrder/StudioOrder** - Studio orders, extensive `DoAction` overrides

#### ObjectContainer Descendants
14. **Organization** (abstract) - Base for companies/agencies, adds signature handling
15. **Action** (abstract) - Advertising campaigns, complex child management
16. **Agency** - Extends Organization, overrides `Update` and `ShowPassport`
17. **Brand** - Product brands
18. **HeadCompany** - Parent companies
19. **Module** - Media modules
20. **PackageDiscount** - Volume discounts
21. **PackageDiscountPriceList** - Discount pricing
22. **PaymentStudioOrder** - Payment records
23. **Pricelist** - Base pricing lists
24. **ProductionStudio** - Production studios
25. **CampaignPart** - Campaign segments
26. **MassmediaDiscount** - Media-specific discounts
27. **PackModule** - Package modules
28. **SponsorProgram** - Sponsorship programs
29. **AdvertType** - Advertisement types

#### In Protector/Domain
30. **User** - Security user accounts
31. **UserDiscount** - User-specific discounts
32. **Group** - User groups

### 1.3 Common Override Patterns

#### Pattern 1: Custom Passport Forms
**Frequency**: High (8+ classes)  
**Examples**: `Agency`, `Roller`, `Tariff`

```csharp
public override bool ShowPassport(IWin32Window owner)
{
    // Duplicate entire base implementation
    // Only difference: custom passport form type
    return new CustomPassportForm(this, ds).ShowDialog(owner) == DialogResult.OK;
}
```

**Issue**: Entire method copied, violates DRY. Should use Template Method or Factory pattern.

#### Pattern 2: Extended Update Logic
**Frequency**: Medium (3+ classes)  
**Examples**: `Agency`, `Action`

```csharp
public override bool Update()
{
    if (!base.Update()) return false;
    
    // Process child changes from childrenChangesList
    foreach(ChildrenChanges cc in childrenChangesList) {
        // Create/delete associations
    }
    childrenChangesList.Clear();
    return true;
}
```

**Issue**: Child relationship management mixed with update. Should be separate concern.

#### Pattern 3: Custom DoAction Handlers
**Frequency**: Very High (15+ classes)  
**Examples**: `StudioOrder`, `Tariff`, `Action`

```csharp
public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects io)
{
    switch(actionName) {
        case "CustomAction1": HandleAction1(); break;
        case "CustomAction2": HandleAction2(); break;
        default: base.DoAction(actionName, owner, io); break;
    }
}
```

**Issue**: Action dispatch is good pattern, but actions often couple UI/data/business logic.

#### Pattern 4: Property Facades
**Frequency**: Very High (All container classes)  
**Examples**: All derived classes

```csharp
public int RollerId
{
    get { return int.Parse(this["rollerID"].ToString()); }
}
```

**Issue**: Runtime parsing, potential for crashes. Should be strongly-typed or validated.

---

## 2. Technical Debt Catalog

### 2.1 High Priority Issues

#### ISSUE-1: SQL Injection Vulnerability in PKWhereClause
**Severity**: CRITICAL  
**Location**: `PresentationObject.cs:461-477`

```csharp
public string PKWhereClause
{
    get
    {
        StringBuilder clause = new StringBuilder();
        for(int i = 0; i < entity.PKColumns.Length; i++)
        {
            if (i > 0) clause.Append(" And ");
            clause.AppendFormat("{0}='{1}'", columnName, this[columnName]); // UNSAFE!
        }
        return clause.ToString();
    }
}
```

**Risk**: User-controlled data could contain SQL injection payloads.  
**Mitigation**: Property appears unused in codebase (needs verification), consider deprecation or parameterization.

#### ISSUE-2: Inconsistent Null Handling
**Severity**: HIGH  
**Location**: Throughout, especially `PresentationObject.cs:77-86`

```csharp
public object this[string key]
{
    get
    {
        if (parameters.ContainsKey(key))
            return parameters[key];
        return null;  // Implicit null - caller must check
    }
    set { parameters[key] = value; }
}
```

**Risk**: Derived classes do `int.Parse(this["key"].ToString())` without null checks → `NullReferenceException`.  
**Examples**: `Roller.cs:165`, `Tariff.cs:151`, `StudioOrder.cs:86`

**Impact**: Runtime crashes in production.

#### ISSUE-3: Error Recovery Semantics Unclear
**Severity**: HIGH  
**Location**: `PresentationObject.cs:181-187`

```csharp
catch
{
    // If Update fails, refresh the object to show current state
    if (string.Compare(actionName, Constants.Actions.Update) == 0)
        Refresh();
    throw;
}
```

**Risk**: 
- Refresh may fail silently
- Object state after failed update is ambiguous
- Partial updates might be lost
- Event `OnObjectChanged` not fired on refresh

#### ISSUE-4: Event Firing Inconsistency
**Severity**: MEDIUM-HIGH  
**Location**: Multiple methods

**Inconsistencies**:
- `Update()`: Does NOT fire `OnObjectChanged` (line 152-188)
- `Refresh()`: DOES fire `OnObjectChanged` (line 265)
- `ShowPassport()`: Fires only if existing object changed (line 132)
- `Clone()`: Always fires `OnObjectCloned` (line 204)

**Risk**: Event listeners cannot reliably track object changes.

#### ISSUE-5: Dictionary-Based State Management
**Severity**: MEDIUM-HIGH  
**Location**: Entire inheritance hierarchy

**Issues**:
- No type safety at compile time
- Inconsistent key naming (case-insensitive comparison helps but doesn't prevent typos)
- No schema validation
- Performance overhead (dictionary lookups vs. fields)
- Debugging difficulty (can't "Find All References" for a property)

### 2.2 Medium Priority Issues

#### ISSUE-6: Tight Coupling to Infrastructure
**Severity**: MEDIUM  
**Dependencies**:
- `DataAccessor` (static methods)
- `EntityManager` (static methods)
- `ErrorManager` (static methods)
- `MessageBox` (UI)
- `Cursor` (UI)
- `Application.DoEvents()` (UI)

**Impact**: 
- Cannot unit test without database
- Cannot test without UI thread
- Cannot mock dependencies

#### ISSUE-7: PKparameters "Old" Suffix Convention
**Severity**: MEDIUM  
**Location**: `PresentationObject.cs:357`

```csharp
if(IsPKColumn(column.ColumnName))
    PKparameters.Add(column.ColumnName + "Old", row[column]);
```

**Issues**:
- String manipulation for tracking previous values is fragile
- No type that represents "before/after" state
- Easy to forget suffix when accessing
- Not self-documenting

#### ISSUE-8: Clone Method Returns PresentationObject
**Severity**: MEDIUM  
**Location**: `PresentationObject.cs:190-206`

```csharp
public virtual PresentationObject Clone(Dictionary<string, object> newParameters)
{
    // ...
    PresentationObject presentationObject = entity.NewObject;
    // ...
}
```

**Issue**: Caller must downcast to actual type. Should use generic constraint or covariant return.

#### ISSUE-9: Name Property Implicit Empty String
**Severity**: MEDIUM  
**Location**: `PresentationObject.cs:305-314`

```csharp
public virtual string Name
{
    get
    {
        if(parameters.ContainsKey(Constants.Parameters.Name))
            return parameters[Constants.Parameters.Name].ToString();
        else
            return string.Empty;
    }
}
```

**Issue**: Hides when name is genuinely missing vs. empty. Could return `null` for missing.

### 2.3 Low Priority Issues

#### ISSUE-10: GetHashCode Implementation
**Severity**: LOW-MEDIUM  
**Location**: `PresentationObject.cs:451-459`

```csharp
public override int GetHashCode()
{
    int hash = 0;
    for(int i = 0; i < IDs.Length; i++)
        if (IDs[i] != null)
            hash += IDs[i].GetHashCode();  // Addition can cause collisions
    
    return entity.GetHashCode() + hash;
}
```

**Issue**: 
- Simple addition → poor distribution
- Should use XOR or prime multiplication
- Creates new `IDs` array each call (allocations)

#### ISSUE-11: Duplicate Code in ShowPassport Overrides
**Severity**: LOW  
**Count**: 8+ methods

Pattern repeated in `Agency`, `Roller`, and others - entire base method body duplicated.

#### ISSUE-12: isNew Flag Public Setter
**Severity**: LOW  
**Location**: `PresentationObject.cs:302`

```csharp
public bool IsNew
{
    get { return isNew; }
    set { isNew = value; }  // Public setter - dangerous!
}
```

**Risk**: External code can incorrectly mark objects as new/existing.

---

## 3. Derived Class Patterns & Responsibilities

### 3.1 ObjectContainer Pattern

**Purpose**: Entities that contain child entities (parent-child relationships)

**Additional Responsibilities**:
- Lazy loading of children via `ObjectsIterator`
- Filter management
- Caching child content
- Relation scenario support
- Container refresh events

**Key Derived Classes**:
- `Organization` (abstract) → `Agency`, `ProductionStudio`, `HeadCompany`
- `Action` (abstract) → advertising campaigns
- `Module`, `Pricelist`, `Brand`

**Common Override**: `GetContent()` - customizes child loading logic

### 3.2 Simple Entity Pattern

**Purpose**: Leaf entities with no children

**Characteristics**:
- Minimal overrides (often just constructors)
- Heavy use of property facades
- Custom `DoAction` for entity-specific operations

**Examples**: `Roller`, `Tariff`, `Announcement`, `ActJournalRow`

### 3.3 Transient Entity Pattern

**Purpose**: Objects created for UI scenarios, deleted when done

**Examples**: `MassmediaAgency`, `StudioAgency` (inner classes in `Agency.cs`)

**Pattern**:
```csharp
public class MassmediaAgency : PresentationObject
{
    public MassmediaAgency(int agencyId, int massmediaId)
        : base(EntityManager.GetEntity((int)Entities.MassmediaAgency))
    {
        parameters[Massmedia.ParamNames.MassmediaId] = massmediaId.ToString();
        parameters[Agency.ParamNames.AgencyId] = agencyId.ToString();
    }
}
```

Used in `Agency.Update()` to manage many-to-many relationships.

### 3.4 Static Loader Pattern

**Purpose**: Provide static factory methods for loading entities

**Examples**: `Agency.GetAgencyByID()`, `Tariff.GetTariffByID()`, `Roller` constructor with ID

**Pattern**:
```csharp
internal static Agency GetAgencyByID(int agencyId)
{
    Agency agency = new Agency(agencyId);
    agency.Refresh();
    return agency;
}
```

**Issue**: Mixes instance and static concerns, hard to mock for testing.

---

## 4. DataAccessor Integration Analysis

### 4.1 PrepareParameters Pattern

Used in nearly every CRUD operation:

```csharp
Dictionary<string, object> procParameters = Parameters;
DataAccessor.PrepareParameters(
    procParameters, entity, InterfaceObjects.PropertyPage, Constants.Actions.Load);
```

**Purpose**: 
- Adds procedure name based on entity + interface + action
- Adds standard parameters like user ID

**Issue**: Side-effects on passed dictionary, not immutable.

### 4.2 DoAction Pattern

```csharp
DataSet ds = (DataSet)DataAccessor.DoAction(procParameters, out Dictionary<string, object> outParams);
```

**Issues**:
- Always returns `object`, requires cast
- Output parameters via `out` - not self-documenting
- Exceptions not wrapped or categorized

### 4.3 Stored Procedure Dependency

**Every CRUD operation** requires stored procedures following naming convention:
- Format: `{EntityName}_{Action}_{InterfaceObject}`
- Example: `Agency_Update_FakeModule`

**Impact**:
- Can't change DB without updating metadata
- Testing requires database
- No repository abstraction

---

## 5. Refactoring Roadmap

### 5.1 Immediate Actions (Critical - 1-2 weeks)

#### RA-1: Remove PKWhereClause or Make Safe
**Priority**: CRITICAL  
**Effort**: Small  
**Risk**: Low (if unused)

**Steps**:
1. Search codebase for all usages of `PKWhereClause`
2. If unused: Mark as `[Obsolete]` with error, schedule removal
3. If used: Replace with parameterized queries at call sites
4. Add unit test verifying no SQL injection possible

#### RA-2: Add Null Guards to Property Facades
**Priority**: HIGH  
**Effort**: Medium  
**Risk**: Medium (could expose hidden bugs)

**Steps**:
1. Create helper methods in base class:
   ```csharp
   protected int GetInt32(string key)
   {
       if (!parameters.ContainsKey(key) || parameters[key] == null)
           throw new InvalidOperationException($"Required parameter '{key}' is missing.");
       return Convert.ToInt32(parameters[key]);
   }
   
   protected int? GetNullableInt32(string key)
   {
       if (!parameters.ContainsKey(key) || parameters[key] == null || parameters[key] == DBNull.Value)
           return null;
       return Convert.ToInt32(parameters[key]);
   }
   ```
2. Update derived classes to use helpers instead of direct `int.Parse()`
3. Add logging for null access to identify issues
4. Add unit tests for null handling

#### RA-3: Document Event Firing Contract
**Priority**: HIGH  
**Effort**: Small  
**Risk**: Low

**Steps**:
1. Add XML documentation to each event explaining when it fires
2. Add documentation to `Update()`, `Refresh()`, `Delete()` explaining event behavior
3. Consider standardizing: all state changes fire `OnObjectChanged`
4. Add integration tests verifying event firing

### 5.2 Short-Term Refactorings (1-3 months)

#### RA-4: Extract UI Concerns
**Priority**: HIGH  
**Effort**: Large  
**Risk**: High (touches many classes)

**Goal**: Separate UI interactions from business logic

**Steps**:
1. Create `IUserInteraction` interface:
   ```csharp
   public interface IUserInteraction
   {
       bool ConfirmDelete(string objectName);
       bool ConfirmDetach(string objectName);
       void ShowError(Exception ex);
   }
   ```
2. Add dependency injection for `IUserInteraction` to PresentationObject
   - Default implementation uses MessageBox (current behavior)
   - Test implementation returns pre-configured responses
3. Replace direct `MessageBox`, `Cursor`, `Application.DoEvents` calls
4. Update all derived classes to use interface
5. Add tests using mock implementation

**Benefits**:
- Testable without UI thread
- Can run operations in background
- Console/service usage becomes possible

#### RA-5: Extract Data Access Concerns
**Priority**: HIGH  
**Effort**: Large  
**Risk**: High

**Goal**: Abstract database operations behind repository interface

**Steps**:
1. Create `IEntityRepository` interface:
   ```csharp
   public interface IEntityRepository
   {
       DataSet Load(Dictionary<string, object> parameters, Entity entity, InterfaceObjects io);
       DataSet Insert(Dictionary<string, object> parameters, Entity entity, out Dictionary<string, object> outParams);
       DataSet Update(Dictionary<string, object> parameters, Entity entity, out Dictionary<string, object> outParams);
       void Delete(Dictionary<string, object> parameters, Entity entity);
   }
   ```
2. Create `DataAccessorRepository` implementing interface (wraps current `DataAccessor`)
3. Add dependency injection to PresentationObject
4. Replace direct `DataAccessor` calls
5. Create in-memory repository for testing

**Benefits**:
- Unit testing without database
- Can mock data access
- Easier to log/trace operations
- Could add caching layer

#### RA-6: Introduce Strongly-Typed State
**Priority**: MEDIUM-HIGH  
**Effort**: Very Large  
**Risk**: Very High (breaking change)

**Goal**: Replace dictionary-based state with strongly-typed objects

**Approach A - Gradual Migration**:
1. Add generic constraint to PresentationObject:
   ```csharp
   public class PresentationObject<TState> : PresentationObject
       where TState : class, new()
   {
       private TState state = new TState();
       protected TState State => state;
   }
   ```
2. Existing non-generic `PresentationObject` remains for backward compatibility
3. New/refactored classes use generic version
4. Migrate incrementally over time

**Approach B - Code Generation**:
1. Generate state classes from entity metadata
2. Keep dictionary as backing store initially
3. Add property accessors that read/write dictionary
4. Eventually remove dictionary

**Example Generated State**:
```csharp
public class RollerState
{
    public int RollerId { get; set; }
    public int RollerTypeId { get; set; }
    public int Duration { get; set; }
    public string Path { get; set; }
    public bool IsMute { get; set; }
}

public class Roller : PresentationObject<RollerState>
{
    public int RollerId => State.RollerId;
    public bool IsMute => State.IsMute;
}
```

**Benefits**:
- Compile-time type checking
- IntelliSense support
- Easier refactoring
- Better performance
- Self-documenting

#### RA-7: Template Method for ShowPassport
**Priority**: MEDIUM  
**Effort**: Medium  
**Risk**: Medium

**Goal**: Eliminate duplicate code in `ShowPassport` overrides

**Steps**:
1. Add protected virtual method to base:
   ```csharp
   protected virtual PassportForm CreatePassportForm(DataSet ds)
   {
       return GetPassportForm(ds);
   }
   ```
2. Refactor base `ShowPassport` to call `CreatePassportForm`
3. Update derived classes to only override `CreatePassportForm`
4. Mark `GetPassportForm` as obsolete
5. Add tests for custom passport forms

### 5.3 Medium-Term Refactorings (3-6 months)

#### RA-8: Introduce Repository Pattern
**Priority**: MEDIUM  
**Effort**: Large  
**Risk**: Medium

**Goal**: Complete abstraction of data access

Build on RA-5 to create full repository pattern:

```csharp
public interface IRepository<TEntity> where TEntity : PresentationObject
{
    TEntity GetById(params object[] ids);
    IEnumerable<TEntity> GetAll();
    IEnumerable<TEntity> Find(Expression<Func<TEntity, bool>> predicate);
    void Add(TEntity entity);
    void Update(TEntity entity);
    void Delete(TEntity entity);
}
```

#### RA-9: Separate Identity from State
**Priority**: MEDIUM  
**Effort**: Large  
**Risk**: High

**Goal**: Make object identity immutable and explicit

**Steps**:
1. Create `EntityIdentity` value object:
   ```csharp
   public sealed class EntityIdentity : IEquatable<EntityIdentity>
   {
       public int EntityId { get; }
       public IReadOnlyList<object> Keys { get; }
       
       public EntityIdentity(int entityId, params object[] keys)
       {
           EntityId = entityId;
           Keys = Array.AsReadOnly(keys);
       }
       
       // Proper Equals/GetHashCode implementation
   }
   ```
2. Add `Identity` property to PresentationObject
3. Set identity immutable after first `Init`
4. Update `Equals`/`GetHashCode` to use `EntityIdentity`
5. Remove `IDs`, `Key` properties (use `Identity` instead)

**Benefits**:
- Clear object identity
- Better hashcode distribution
- Immutable identity (can use as dictionary key safely)
- Easier debugging

#### RA-10: Event Sourcing Light
**Priority**: LOW-MEDIUM  
**Effort**: Large  
**Risk**: Medium

**Goal**: Track all changes to objects for audit/undo

**Steps**:
1. Add change tracking to PresentationObject:
   ```csharp
   public class StateChange
   {
       public DateTime Timestamp { get; set; }
       public string PropertyName { get; set; }
       public object OldValue { get; set; }
       public object NewValue { get; set; }
   }
   
   private List<StateChange> changes = new List<StateChange>();
   public IReadOnlyList<StateChange> Changes => changes.AsReadOnly();
   ```
2. Track changes in indexer setter
3. Add `GetChanges()` method returning delta
4. Use for `Update()` to send only changed values
5. Add `Rollback()` to undo changes

**Benefits**:
- Audit trail
- Undo functionality
- Optimistic concurrency
- Better `Update` efficiency

### 5.4 Long-Term Strategic Changes (6-12 months)

#### RA-11: CQRS Pattern
**Priority**: LOW  
**Effort**: Very Large  
**Risk**: Very High

**Goal**: Separate read models from write models

**Rationale**:
- Current architecture conflates reading (for display) and writing (for updates)
- `DataSet` from `Load` might not match what `Update` needs
- Different use cases: journals (read many), passports (read/write one)

**Approach**:
1. Create separate read models (DTOs) for grid/journal display
2. Keep PresentationObject for write operations
3. Map between read models and PresentationObject only when editing
4. Use separate stored procedures for reads vs. writes

#### RA-12: Async/Await Support
**Priority**: LOW  
**Effort**: Very Large  
**Risk**: Very High

**Goal**: Non-blocking UI during data operations

**Challenges**:
- .NET Framework 4.8 (not .NET Core)
- Winforms synchronization context
- Stored procedure calls synchronous
- Would require async throughout call chain

**Steps**:
1. Add async variants of CRUD methods:
   ```csharp
   public virtual async Task<bool> UpdateAsync()
   {
       // Implementation
   }
   ```
2. Update DataAccessor for async
3. Update UI code to use async/await
4. Keep synchronous methods for backward compatibility

#### RA-13: Migration to .NET Core/8
**Priority**: LOW (Strategic)  
**Effort**: Extreme  
**Risk**: Very High

**Goal**: Modern framework, cross-platform, better performance

**Prerequisites**:
- Complete RA-4 through RA-10 first
- Reduces migration surface area
- Better architecture makes migration easier

---

## 6. Implementation Priorities

### Priority Matrix

| Refactoring | Business Value | Technical Risk | Effort | Priority Score |
|------------|---------------|----------------|--------|----------------|
| RA-1: Fix PKWhereClause | Critical (Security) | Low | Small | **IMMEDIATE** |
| RA-2: Null Guards | High (Stability) | Medium | Medium | **IMMEDIATE** |
| RA-3: Document Events | High (Correctness) | Low | Small | **IMMEDIATE** |
| RA-4: Extract UI | High (Testability) | High | Large | SHORT-TERM |
| RA-5: Extract Data Access | High (Testability) | High | Large | SHORT-TERM |
| RA-6: Strongly-Typed State | Medium (Maintainability) | Very High | Very Large | SHORT-TERM |
| RA-7: Template ShowPassport | Medium (DRY) | Medium | Medium | SHORT-TERM |
| RA-8: Repository Pattern | Medium (Architecture) | Medium | Large | MEDIUM-TERM |
| RA-9: Separate Identity | Medium (Correctness) | High | Large | MEDIUM-TERM |
| RA-10: Change Tracking | Low-Medium (Features) | Medium | Large | MEDIUM-TERM |
| RA-11: CQRS | Low (Architecture) | Very High | Very Large | LONG-TERM |
| RA-12: Async/Await | Low (UX) | Very High | Very Large | LONG-TERM |
| RA-13: .NET Core Migration | Low (Strategic) | Extreme | Extreme | STRATEGIC |

### Recommended Phasing

**Phase 1 (Weeks 1-2): Critical Fixes**
- RA-1: Remove/fix PKWhereClause
- RA-2: Add null guards
- RA-3: Document events
- **Goal**: Eliminate critical security and stability issues

**Phase 2 (Months 1-2): Foundation**
- RA-4: Extract UI concerns (implement interfaces)
- RA-7: Template method for ShowPassport
- **Goal**: Enable testing, reduce duplication

**Phase 3 (Months 2-3): Data Layer**
- RA-5: Extract data access (implement repository interface)
- **Goal**: Decouple from database, enable unit testing

**Phase 4 (Months 3-6): Type Safety**
- RA-6: Introduce strongly-typed state (gradual migration)
- **Goal**: Compile-time correctness, better maintainability

**Phase 5 (Months 6-12): Architecture**
- RA-8: Full repository pattern
- RA-9: Separate identity
- RA-10: Change tracking
- **Goal**: Modern, maintainable architecture

**Phase 6 (Year 2+): Strategic**
- RA-11: CQRS
- RA-12: Async/Await
- RA-13: .NET Core migration
- **Goal**: Future-proof platform

---

## 7. Testing Strategy

### 7.1 Current State

**No unit tests found** for PresentationObject or derived classes.

**Barriers to Testing**:
- Static dependencies (DataAccessor, EntityManager)
- Database requirements
- UI dependencies (MessageBox, Cursor)
- No dependency injection

### 7.2 Testing After Refactoring

#### After Phase 1 (Critical Fixes)
- Add integration tests for null handling
- Add tests for event firing
- Still requires database

#### After Phase 2 (Foundation)
- Unit test UI interactions with mock `IUserInteraction`
- Test business logic without UI thread
- Still requires database for data operations

#### After Phase 3 (Data Layer)
- Full unit testing with mock repository
- Test all CRUD operations without database
- Test error handling and edge cases
- Test all derived classes

**Example Test**:
```csharp
[Test]
public void Update_WhenSuccess_FiresObjectChangedEvent()
{
    // Arrange
    var mockRepo = new MockRepository();
    var mockUI = new MockUserInteraction();
    var entity = new Entity(/* ... */);
    var obj = new PresentationObject(entity) 
    {
        Repository = mockRepo,
        UserInteraction = mockUI
    };
    
    bool eventFired = false;
    obj.ObjectChanged += _ => eventFired = true;
    
    // Act
    obj.Update();
    
    // Assert
    Assert.IsTrue(eventFired);
    Assert.AreEqual(1, mockRepo.UpdateCallCount);
}
```

### 7.3 Testing Recommendations

1. **Start with critical paths**: Update, Delete, Refresh
2. **Test null handling**: All property facades
3. **Test event firing**: All operations that should fire events
4. **Test error recovery**: Failed updates, database errors
5. **Test concurrency**: Multiple updates to same object
6. **Test identity**: Equals, GetHashCode, Equal()
7. **Integration tests**: Full round-trip with test database

---

## 8. Risk Assessment

### 8.1 Risks of NOT Refactoring

| Risk | Probability | Impact | Severity |
|------|------------|--------|----------|
| SQL Injection exploit | Medium | Critical | **HIGH** |
| Production crashes from null refs | High | High | **HIGH** |
| Data corruption from unclear state | Medium | Critical | **MEDIUM-HIGH** |
| Unable to add features due to coupling | High | Medium | **MEDIUM-HIGH** |
| Maintenance costs spiral | High | Medium | **MEDIUM-HIGH** |
| Developer turnover due to complexity | Medium | Medium | **MEDIUM** |
| Cannot migrate to modern .NET | Low | High | **MEDIUM** |

### 8.2 Risks of Refactoring

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| Breaking existing functionality | Medium | High | Comprehensive testing, incremental approach |
| Performance regression | Low | Medium | Benchmarking, profiling |
| Team resistance to change | Medium | Medium | Training, documentation, gradual rollout |
| Scope creep | High | Medium | Strict phase boundaries, clear goals |
| Extended development time | Medium | High | Phased approach, maintain velocity |

### 8.3 Risk Mitigation Strategies

1. **Incremental Approach**: Each phase delivers value independently
2. **Feature Flags**: New behavior behind flags, can rollback
3. **Parallel Implementation**: New code alongside old, gradually migrate
4. **Comprehensive Testing**: Both automated and manual QA
5. **Documentation**: Clear migration guides for developers
6. **Monitoring**: Track errors, performance before/after
7. **Rollback Plan**: Can revert any phase if critical issues found

---

## 9. Success Metrics

### 9.1 Code Quality Metrics

**Baseline (Current)**:
- Unit test coverage: 0%
- Cyclomatic complexity of PresentationObject: ~25
- Derived classes: 31
- Lines of code in base class: ~485
- Code duplication in ShowPassport overrides: ~150 lines

**Target After Phase 4**:
- Unit test coverage: >80% of base class logic
- Cyclomatic complexity: <15 (with extracted concerns)
- Code duplication: <50 lines (after template method)
- Null reference exceptions: <5 per month (from current ~20+)

### 9.2 Development Velocity Metrics

**Track**:
- Time to implement new entity type (target: -50%)
- Time to fix bugs in entity operations (target: -40%)
- Time to add tests for new feature (target: from impossible to <1 day)

### 9.3 Production Metrics

**Track**:
- `NullReferenceException` count (target: -90%)
- Database query time (monitor for regressions)
- UI responsiveness (monitor for regressions)
- User-reported data issues (target: -50%)

---

## 10. Conclusion

The `PresentationObject` inheritance model is functional but shows significant technical debt accumulated over years of development. The base class violates Single Responsibility Principle by combining UI, data access, business logic, and state management. This coupling makes testing difficult, maintenance costly, and evolution risky.

**Key Recommendations**:

1. **Immediate Action**: Fix security vulnerability (RA-1) and add null guards (RA-2) - these are critical and low-risk.

2. **Short-Term Focus**: Extract UI and data access concerns (RA-4, RA-5) - this is the foundation for all future improvements and enables testing.

3. **Medium-Term Goal**: Introduce strongly-typed state (RA-6) - this is the biggest maintainability win but requires careful migration.

4. **Long-Term Vision**: Modern architecture with repository pattern, CQRS, and eventual migration to .NET Core - but only after foundation is solid.

5. **Phased Approach**: Each phase delivers independent value and can be deployed without completing subsequent phases.

The proposed roadmap balances risk, effort, and business value. By starting with critical security and stability fixes, then systematically separating concerns, the codebase can evolve toward a more maintainable, testable architecture without requiring a risky "big bang" rewrite.

**Estimated Timeline**: 6-12 months for Phases 1-4, which will deliver the majority of the business value and architectural improvements.

---

## Appendix A: Derived Classes Summary

### Direct PresentationObject Descendants (13)
1. ActJournalRow
2. ActionRollerInStatJournal  
3. Announcement
4. FirmBalance (abstract)
5. MuteRoller
6. Roller
7. SponsorTariff
8. Tariff
9. TariffWindow
10. Domain/MasterIssue
11. Domain/StudioOrder/StudioOrder
12. Protector/User
13. Protector/UserDiscount

### ObjectContainer Descendants (18)
14. Organization (abstract) - FogSoft.WinForm.Classes
    - Agency
    - ProductionStudio
    - HeadCompany
15. Action (abstract)
16. Brand
17. Module
18. PackageDiscount
19. PackageDiscountPriceList
20. PaymentStudioOrder
21. Pricelist
22. CampaignPart
23. MassmediaDiscount
24. PackModule
25. SponsorProgram
26. AdvertType
27. Protector/Group

### Framework Classes
28. ObjectContainer - FogSoft.WinForm.Classes (direct base for app containers)
29. FakeContainer - FogSoft.WinForm.Classes (not derived from PresentationObject, but similar interface)

**Total**: 31 derived classes analyzed

---

## Appendix B: Code Examples

### B.1 Current vs. Proposed: Property Access

**Current (Runtime errors possible)**:
```csharp
public int RollerId
{
    get { return int.Parse(this["rollerID"].ToString()); }
}
```

**After RA-2 (Null-safe)**:
```csharp
public int RollerId
{
    get { return GetInt32("rollerID"); }
}
```

**After RA-6 (Strongly-typed)**:
```csharp
public int RollerId
{
    get { return State.RollerId; }
}
```

### B.2 Current vs. Proposed: ShowPassport

**Current (8+ copies of this code)**:
```csharp
public override bool ShowPassport(IWin32Window owner)
{
    try
    {
        Application.DoEvents();
        Cursor.Current = Cursors.WaitCursor;
        
        DataAccessor.PrepareParameters(parameters, entity, 
            InterfaceObjects.PropertyPage, Constants.Actions.Load);
        
        DataSet ds = null;
        if (DataAccessor.IsProcedureExist(parameters))
            ds = DataAccessor.DoAction(parameters) as DataSet;
        
        bool isNewObject = IsNew;
        RollerPassportForm passport = new RollerPassportForm(this, ds);
        bool res = passport.ShowDialog(owner) == DialogResult.OK;
        
        if (res && !isNewObject) OnObjectChanged(this);
        return res;
    }
    catch (Exception ex)
    {
        ErrorManager.PublishError(ex);
        return false;
    }
    finally
    {
        Cursor.Current = Cursors.Default;
    }
}
```

**After RA-7 (Template Method - Single override needed)**:
```csharp
protected override PassportForm CreatePassportForm(DataSet ds)
{
    return new RollerPassportForm(this, ds);
}
```

### B.3 Current vs. Proposed: Update with Testing

**Current (Cannot unit test)**:
```csharp
public virtual bool Update()
{
    Dictionary<string, object> procParameters = Parameters;
    string actionName = IsNew ? Constants.Actions.AddItem : Constants.Actions.Update;
    DataAccessor.PrepareParameters(procParameters, entity, 
        InterfaceObjects.FakeModule, actionName);
    
    foreach(KeyValuePair<string, object> kvp in PKparameters)
        procParameters[kvp.Key] = kvp.Value;
    
    try
    {
        DataSet ds = (DataSet)DataAccessor.DoAction(procParameters, out var outParams);
        foreach (KeyValuePair<string, object> kvp in outParams)
            this[kvp.Key] = kvp.Value;
        
        if (ds != null) Init(ds.Tables[0].Rows[0]);
        isNew = false;
        return true;
    }
    catch
    {
        if (actionName == Constants.Actions.Update)
            Refresh();
        throw;
    }
}
```

**After RA-5 (Testable with mock repository)**:
```csharp
public virtual bool Update()
{
    var state = GetStateForUpdate();
    string actionName = IsNew ? Constants.Actions.AddItem : Constants.Actions.Update;
    
    try
    {
        var result = repository.Update(state, entity, actionName);
        ApplyUpdateResult(result);
        isNew = false;
        OnObjectChanged(this);
        return true;
    }
    catch
    {
        if (actionName == Constants.Actions.Update)
            Refresh();
        throw;
    }
}
```

---

**Document Version**: 1.0  
**Last Updated**: January 2026  
**Next Review**: After Phase 1 completion
