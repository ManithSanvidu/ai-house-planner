import requests
import time

URL = "http://localhost:5265/api/v1/ai-generation/generate"

def test_case(name, payload, expect_success=True):
    print(f"\n--- Testing Case: {name} ---")
    resp = requests.post(URL, json=payload)
    if not resp.ok:
        print(f"FAILED (Backend rejection as expected? {not expect_success})")
        print(resp.status_code, resp.text)
        return

    if not expect_success:
        print("WARNING: Expected failure but succeeded!")
        return

    data = resp.json()
    wid = data.get("workflowId")
    print(f"Workflow ID: {wid}")

    print("Polling...")
    for _ in range(15):
        time.sleep(2)
        st = requests.get(f"http://localhost:5265/api/v1/workflows/{wid}/status")
        if not st.ok:
            continue
        st_data = st.json()
        print(f"Status: {st_data.get('status')}")
        if st_data.get('status') == 'failed':
            print("WORKFLOW FAILED")
            print(st_data)
            return
        if st_data.get('status') == 'design_generated' or st_data.get('design'):
            design = st_data.get('design')
            print("DESIGN GENERATED!")
            rooms = design.get('rooms', [])
            print(f"Floor count: {design.get('floorCount')}")
            print("Rooms:")
            for r in rooms:
                print(f" - {r.get('name') or r.get('roomType')}")
            
            bed_count = sum(1 for r in rooms if 'bed' in r.get('roomType', '').lower())
            bath_count = sum(1 for r in rooms if 'bath' in r.get('roomType', '').lower())
            print(f"Bedrooms: {bed_count}")
            print(f"Bathrooms: {bath_count}")
            return

    print("Timeout.")


if __name__ == "__main__":
    test_case("Case A (15 perch, 2 bed, 1 bath)", {
        "landSizeCategory": "small",
        "landSizePerches": 15,
        "bedrooms": 2,
        "bathrooms": 1,
        "houseType": "modern"
    })

    test_case("Case B (25 perch, 3 bed, 2 bath)", {
        "landSizeCategory": "medium",
        "landSizePerches": 25,
        "bedrooms": 3,
        "bathrooms": 2,
        "houseType": "modern"
    })

    test_case("Case C (15 perch, 4 bed)", {
        "landSizeCategory": "small",
        "landSizePerches": 15,
        "bedrooms": 4,
        "bathrooms": 2,
        "houseType": "modern"
    }, expect_success=False)
