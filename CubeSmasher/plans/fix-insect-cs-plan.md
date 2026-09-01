# План: Исправление Insect.cs

## Описание проблемы

Две ошибки в [`Insect.Update()`](Assets/Scripts/Enemy/Insect.cs):

### 1. Некорректная проверка `_targetPosition == null` (строка 17)

`Vector3` — это **структура** (value type) в C#, она никогда не может быть `null`.  
Условие `if (_targetPosition == null)` всегда **false**, поэтому насекомое сразу начинает лететь к цели, даже если `Move()` ещё не был вызван.

**Исправление:** Добавить булев флаг `_isMoving`, который устанавливается в `true` при вызове `Move()`.

### 2. Странное поведение при достижении цели (строка 24)

`transform.Translate(transform.position * 10)` — умножает текущую позицию на 10 и сдвигает объект. Это выглядит как баг/заглушка.

**Исправление:** Просто уничтожать объект сразу при достижении цели (без лишних трансформаций).

## Изменения в коде

```csharp
public class Insect : MonoBehaviour
{
    [SerializeField] private float _speed;

    private Vector3 _targetPosition;
    private bool _isMoving;

    public void Move(Transform target)
    {
        _targetPosition = target.position;
        _isMoving = true;
    }

    private void Update()
    {
        if (!_isMoving)                          // было: _targetPosition == null
            return;

        transform.position = Vector3.MoveTowards(transform.position, _targetPosition, _speed * Time.deltaTime);

        if (transform.position == _targetPosition)
        {
            Destroy(gameObject);                 // было: transform.Translate(...) + Destroy(1f)
        }
    }
}
```

## Файл для изменения

- [`Assets/Scripts/Enemy/Insect.cs`](Assets/Scripts/Enemy/Insect.cs)