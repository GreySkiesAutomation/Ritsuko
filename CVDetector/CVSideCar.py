import cv2
import threading
import time

from fastapi import FastAPI
import uvicorn

app = FastAPI()

presence_detected = False
last_motion_timestamp = 0.0

PRESENCE_TIMEOUT_SECONDS = 8
CAMERA_INDEX = 0

PIXEL_DIFF_THRESHOLD = 35
CHANGED_PIXELS_THRESHOLD = 12000
MIN_CONTOUR_AREA = 1200


def camera_loop():
    global presence_detected
    global last_motion_timestamp

    print("[CVSideCar] Opening camera...")
    cap = cv2.VideoCapture(CAMERA_INDEX)

    if not cap.isOpened():
        raise RuntimeError(f"[CVSideCar] Failed to open camera at index {CAMERA_INDEX}")

    ret1, frame1 = cap.read()
    ret2, frame2 = cap.read()

    if not ret1 or not ret2 or frame1 is None or frame2 is None:
        raise RuntimeError("[CVSideCar] Failed to read initial frames from camera")

    print("[CVSideCar] Camera loop started.")

    while True:
        diff = cv2.absdiff(frame1, frame2)
        gray = cv2.cvtColor(diff, cv2.COLOR_BGR2GRAY)

        blur = cv2.GaussianBlur(gray, (11, 11), 0)
        _, thresh = cv2.threshold(blur, PIXEL_DIFF_THRESHOLD, 255, cv2.THRESH_BINARY)

        dilated = cv2.dilate(thresh, None, iterations=2)
        contours, _ = cv2.findContours(dilated, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)

        filtered_mask = thresh.copy()
        filtered_mask[:, :] = 0

        for contour in contours:
            contour_area = cv2.contourArea(contour)

            if contour_area < MIN_CONTOUR_AREA:
                continue

            cv2.drawContours(filtered_mask, [contour], -1, 255, thickness=cv2.FILLED)

        changed_pixels = cv2.countNonZero(filtered_mask)

        print(f"[CVSideCar] changed_pixels={changed_pixels}")

        if changed_pixels > CHANGED_PIXELS_THRESHOLD:
            last_motion_timestamp = time.time()

        presence_detected = (time.time() - last_motion_timestamp) < PRESENCE_TIMEOUT_SECONDS

        frame1 = frame2
        ret2, frame2 = cap.read()

        if not ret2 or frame2 is None:
            print("[CVSideCar] Warning: failed to read frame from camera.")
            time.sleep(0.25)
            continue

        time.sleep(0.05)


@app.get("/presence")
def get_presence():
    return {
        "presence": presence_detected,
        "seconds_since_motion": time.time() - last_motion_timestamp
    }


def start_camera_thread():
    thread = threading.Thread(target=camera_loop, daemon=True)
    thread.start()


if __name__ == "__main__":
    print("[CVSideCar] Starting service...")
    start_camera_thread()

    uvicorn.run(
        app,
        host="127.0.0.1",
        port=8002
    )
