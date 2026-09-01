# План: Исправление спавна насекомых по клику мыши

## Анализ проблемы

В [`ClickHandler.Click()`](Assets/Scripts/ClickHandler.cs:22) вызывается:
```csharp
_insectSpawner.Spawn(_camera.ScreenToWorldPoint(Input.mousePosition));
```

`Input.mousePosition` возвращает `Vector3` с `z = 0`.  
`ScreenToWorldPoint` с `z = 0` возвращает позицию **камеры**, а не точку на игровой плоскости.

Затем в [`InsectSpawner.Create()`](Assets/Scripts/Enemy/InsectSpawner.cs:17) Z-координата переопределяется:
```csharp
return Instantiate(prefab, new Vector3(spawnPosition.x, spawnPosition.y, transform.position.z), Quaternion.identity);
```

В результате насекомое спавнится на позиции самого спавнера по Z, а не в точке клика.

**Дополнительные баги:**
1. [`Insect.Update()`](Assets/Scripts/Enemy/Insect.cs:17) — проверка `_targetPosition == null` некорректна, т.к. `Vector3` — структура (value type).
2. [`Insect.Update()`](Assets/Scripts/Enemy/Insect.cs:24) — `transform.Translate(transform.position * 10)` — странное поведение при достижении цели.

---

## Пункты плана

### 1. ClickHandler.cs — исправить `ScreenToWorldPoint`

**Проблема:** `Input.mousePosition.z = 0`, из-за чего `ScreenToWorldPoint` возвращает позицию камеры.

**Решение:** Задать Z-компоненту как расстояние (depth) от камеры до игровой плоскости (обычно это `-Camera.main.transform.position.z`, т.е. абсолютное расстояние).

```csharp
Vector3 mousePos = Input.mousePosition;
mousePos.z = -_camera.transform.position.z; // или _camera.nearClipPlane + distance
_insectSpawner.Spawn(_camera.ScreenToWorldPoint(mousePos));
```

### 2. InsectSpawner.Create() — убрать переопределение Z

**Проблема:** `new Vector3(spawnPosition.x, spawnPosition.y, transform.position.z)` заменяет корректную Z на Z самого спавнера.

**Решение:** Использовать `spawnPosition` напрямую, без изменений:

```csharp
return Instantiate(prefab, spawnPosition, Quaternion.identity);
```

### 3. Insect.Update() — исправить проверку `_targetPosition == null`

**Проблема:** `Vector3` — это структура (struct), она никогда не равна `null`. Условие всегда `false`.

**Решение:** Добавить булев флаг `_isMoving` или проверять `_targetPosition == Vector3.zero` (если `Vector3.zero` — невалидная позиция).

Вариант с флагом:
```csharp
private bool _isMoving;

public void Move(Transform target)
{
    _targetPosition = target.position;
    _isMoving = true;
}

private void Update()
{
    if (!_isMoving) return;
    // ...
}
```

### 4. Insect.Update() — исправить поведение при достижении цели

**Проблема:** `transform.Translate(transform.position * 10)` — умножает позицию на 10 и двигает объект, что выглядит как баг/заглушка.

**Решение:** Просто уничтожить объект при достижении цели (или запустить анимацию/эффект и затем уничтожить):

```csharp
if (transform.position == _targetPosition)
{
    Destroy(gameObject);
}
```

---

## Диаграмма потока данных (исправленная)

```mermaid
flowchart LR
    A[ClickHandler.Click] --> B[Input.mousePosition]
    B --> C[Set mousePos.z = distance to game plane]
    C --> D[Camera.ScreenToWorldPoint]
    D --> E[InsectSpawner.Spawn worldPosition]
    E --> F[Instantiate prefab at worldPosition]
    F --> G[Insect.Move to target]
```

---

## Файлы для изменения

| Файл | Изменения |
|------|-----------|
| [`Assets/Scripts/ClickHandler.cs`](Assets/Scripts/ClickHandler.cs) | Добавить Z-depth в `ScreenToWorldPoint` (строка 22) |
| [`Assets/Scripts/Enemy/InsectSpawner.cs`](Assets/Scripts/Enemy/InsectSpawner.cs) | Убрать `new Vector3(...)`, использовать `spawnPosition` напрямую (строка 17) |
| [`Assets/Scripts/Enemy/Insect.cs`](Assets/Scripts/Enemy/Insect.cs) | Исправить проверку `_targetPosition == null` (строка 17); исправить `transform.Translate` (строка 24) |